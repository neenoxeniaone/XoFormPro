using Microsoft.AspNetCore.DataProtection;

namespace FormSubmissionPro.Api.Services.Security;

public interface ISecretProtector
{
    string Protect(string? plaintext);
    string Unprotect(string? stored);
}

/// <summary>
/// Encrypts secrets (Graph client secret, captcha secrets) at rest using ASP.NET Data
/// Protection. Encrypted values carry an "enc:" prefix; values without it are treated as
/// legacy plaintext (e.g. set directly via SQL) and returned as-is — so both work.
/// </summary>
public class SecretProtector : ISecretProtector
{
    private const string Prefix = "enc:";
    private readonly IDataProtector _protector;

    public SecretProtector(IDataProtectionProvider provider) =>
        _protector = provider.CreateProtector("fsp.secrets.v1");

    public string Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext ?? "";
        if (plaintext.StartsWith(Prefix)) return plaintext; // already encrypted
        return Prefix + _protector.Protect(plaintext);
    }

    public string Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return stored ?? "";
        if (!stored.StartsWith(Prefix)) return stored; // legacy plaintext
        try { return _protector.Unprotect(stored[Prefix.Length..]); }
        catch { return stored; }
    }
}
