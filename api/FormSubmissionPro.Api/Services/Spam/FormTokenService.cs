using System.Security.Cryptography;
using System.Text;
using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Services.Spam;

public record IssuedToken(string Token, DateTimeOffset ExpiresAt);

public record TokenVerdict(bool Valid, string? Reason)
{
    public static readonly TokenVerdict Ok = new(true, null);
    public static TokenVerdict Fail(string reason) => new(false, reason);
}

public interface IFormTokenService
{
    /// <summary>Issues a single-use token bound to a form and the origin that asked for it.</summary>
    Task<IssuedToken> IssueAsync(Form form, string? origin, CancellationToken ct = default);

    /// <summary>Verifies signature, origin, age, and single use — burning the nonce on success.</summary>
    Task<TokenVerdict> VerifyAsync(Form form, string? token, string? origin, CancellationToken ct = default);
}

/// <summary>
/// Proves a submission came from a real page load rather than a direct POST.
///
/// The token is "{formId}.{nonce}.{issuedAtUnix}.{hmac}", signed with the app's JWT key.
/// The signature makes it unforgeable, the nonce row makes it single-use, and the age
/// bounds reject both the instant replay and the stale captured payload.
/// </summary>
public class FormTokenService : IFormTokenService
{
    /// <summary>Nobody types a genuine enquiry in under three seconds.</summary>
    private static readonly TimeSpan MinAge = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(2);

    private readonly AppDbContext _db;
    private readonly byte[] _key;

    public FormTokenService(AppDbContext db, Auth.JwtOptions jwt)
    {
        _db = db;
        // Derived from the JWT key so there is one secret to configure, not two.
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(jwt.SigningKey + "|form-token.v1"));
    }

    public async Task<IssuedToken> IssueAsync(Form form, string? origin, CancellationToken ct = default)
    {
        var nonce = Guid.NewGuid();
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.Add(MaxAge);

        _db.FormTokens.Add(new FormToken
        {
            Id = nonce,
            FormId = form.Id,
            Origin = origin ?? "",
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
        });
        await _db.SaveChangesAsync(ct);

        var payload = Payload(form.Id, nonce, issuedAt.ToUnixTimeSeconds(), origin);
        return new IssuedToken($"{form.Id:N}.{nonce:N}.{issuedAt.ToUnixTimeSeconds()}.{Sign(payload)}", expiresAt);
    }

    public async Task<TokenVerdict> VerifyAsync(Form form, string? token, string? origin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return TokenVerdict.Fail("no token supplied");

        var parts = token.Split('.');
        if (parts.Length != 4) return TokenVerdict.Fail("malformed token");
        if (!Guid.TryParseExact(parts[0], "N", out var formId) || formId != form.Id)
            return TokenVerdict.Fail("token issued for a different form");
        if (!Guid.TryParseExact(parts[1], "N", out var nonce))
            return TokenVerdict.Fail("malformed nonce");
        if (!long.TryParse(parts[2], out var issuedUnix))
            return TokenVerdict.Fail("malformed timestamp");

        // The stored row carries the issuing origin, so the signature is checked against it
        // rather than against whatever origin the submission claims.
        var row = await _db.FormTokens.FirstOrDefaultAsync(t => t.Id == nonce && t.FormId == form.Id, ct);
        if (row is null) return TokenVerdict.Fail("unknown token");

        var expected = Sign(Payload(form.Id, nonce, issuedUnix, string.IsNullOrEmpty(row.Origin) ? null : row.Origin));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(parts[3])))
            return TokenVerdict.Fail("bad signature");

        if (row.UsedAt is not null) return TokenVerdict.Fail("token already used");

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(issuedUnix);
        if (age < MinAge) return TokenVerdict.Fail($"submitted {age.TotalSeconds:0.0}s after page load");
        if (age > MaxAge) return TokenVerdict.Fail("token expired");

        var issuingHost = OriginMatcher.HostOf(row.Origin, null);
        var submittingHost = OriginMatcher.HostOf(origin, null);
        if (issuingHost is not null && !string.Equals(issuingHost, submittingHost, StringComparison.OrdinalIgnoreCase))
            return TokenVerdict.Fail($"origin changed since issue ({issuingHost} → {submittingHost ?? "none"})");

        // Burn it.
        row.UsedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return TokenVerdict.Ok;
    }

    private static string Payload(Guid formId, Guid nonce, long issuedUnix, string? origin) =>
        $"{formId:N}|{nonce:N}|{issuedUnix}|{origin?.ToLowerInvariant() ?? ""}";

    private string Sign(string payload) =>
        Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
}
