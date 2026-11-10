using System.Security.Cryptography;
using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Services.Auth;

public record GeneratedApiKey(string FullKey, string Prefix, string Hash);

public interface IApiKeyService
{
    GeneratedApiKey Generate();
    string Hash(string fullKey);
    /// <summary>Validates a raw key from an Authorization header; returns the owning org id or null.</summary>
    Task<Guid?> AuthenticateAsync(string? authorizationHeader, CancellationToken ct = default);
}

/// <summary>Issues and validates Submissions-API keys (format: w3f_live_{random}).</summary>
public class ApiKeyService : IApiKeyService
{
    private const string PrefixLabel = "w3f_live_";
    private readonly IServiceScopeFactory _scopeFactory;

    public ApiKeyService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public GeneratedApiKey Generate()
    {
        // 32 random bytes → url-safe base64 secret.
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "").Replace("/", "").Replace("=", "");
        var full = PrefixLabel + secret;
        var prefix = full[..Math.Min(full.Length, 16)] + "…";
        return new GeneratedApiKey(full, prefix, Hash(full));
    }

    public string Hash(string fullKey)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fullKey));
        return Convert.ToHexString(bytes);
    }

    public async Task<Guid?> AuthenticateAsync(string? authorizationHeader, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader)) return null;
        var raw = authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader["Bearer ".Length..].Trim()
            : authorizationHeader.Trim();
        if (!raw.StartsWith(PrefixLabel, StringComparison.Ordinal)) return null;

        var hash = Hash(raw);
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == hash && k.RevokedAt == null, ct);
        if (key is null) return null;

        key.LastUsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return key.OrganizationId;
    }
}
