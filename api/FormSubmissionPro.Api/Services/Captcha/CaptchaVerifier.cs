using System.Text.Json.Serialization;

namespace FormSubmissionPro.Api.Services.Captcha;

public interface ICaptchaVerifier
{
    /// <summary>Verifies a captcha token. Returns true if the provider confirms it (or provider is "none").</summary>
    Task<bool> VerifyAsync(string provider, string? secret, string? token, string? remoteIp, CancellationToken ct = default);
}

/// <summary>
/// Verifies hCaptcha and Cloudflare Turnstile tokens. Both expose a siteverify endpoint
/// that accepts {secret, response, remoteip} as form data and returns {"success": bool}.
/// </summary>
public class CaptchaVerifier : ICaptchaVerifier
{
    private const string HCaptchaUrl = "https://api.hcaptcha.com/siteverify";
    private const string TurnstileUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<CaptchaVerifier> _logger;

    public CaptchaVerifier(IHttpClientFactory httpFactory, ILogger<CaptchaVerifier> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string provider, string? secret, string? token, string? remoteIp, CancellationToken ct = default)
    {
        var url = provider?.ToLowerInvariant() switch
        {
            "hcaptcha" => HCaptchaUrl,
            "turnstile" => TurnstileUrl,
            _ => null,
        };

        if (url is null) return true;                       // captcha not configured → pass
        if (string.IsNullOrWhiteSpace(secret)) return false; // misconfigured → fail closed
        if (string.IsNullOrWhiteSpace(token)) return false;  // user submitted no token

        var form = new Dictionary<string, string>
        {
            ["secret"] = secret,
            ["response"] = token,
        };
        if (!string.IsNullOrWhiteSpace(remoteIp)) form["remoteip"] = remoteIp;

        try
        {
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var resp = await client.PostAsync(url, new FormUrlEncodedContent(form), ct);
            var result = await resp.Content.ReadFromJsonAsync<SiteVerifyResponse>(ct);
            return result?.Success ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Captcha verification request failed for provider {Provider}.", provider);
            return false; // fail closed on network/provider error
        }
    }

    private sealed record SiteVerifyResponse([property: JsonPropertyName("success")] bool Success);
}
