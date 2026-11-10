using System.Security.Cryptography;

namespace FormSubmissionPro.Api.Services.Auth;

/// <summary>Generates opaque random tokens and their SHA-256 hashes (for reset/verify/invite links).</summary>
public static class SecureTokens
{
    public static (string raw, string hash) Generate()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
        return (raw, Hash(raw));
    }

    public static string Hash(string raw)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
