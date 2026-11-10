using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using FormSubmissionPro.Api.Domain;

namespace FormSubmissionPro.Api.Services.Email;

/// <summary>
/// Sends mail through Microsoft Graph using the OAuth2 client-credentials flow
/// (Azure AD app registration with the application permission Mail.Send).
/// Settings come from the database (<see cref="IEmailConfigProvider"/>). Sends as the
/// configured tenant mailbox; the visitor's address is set as Reply-To.
/// </summary>
public class GraphEmailSender : IEmailSender
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IEmailConfigProvider _config;
    private readonly Security.ISecretProtector _secrets;
    private readonly ILogger<GraphEmailSender> _logger;

    // Token cache keyed by client id, so rotating credentials in the DB invalidates it.
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _token;
    private string? _tokenForClientId;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public GraphEmailSender(
        IHttpClientFactory httpFactory,
        IEmailConfigProvider config,
        Security.ISecretProtector secrets,
        ILogger<GraphEmailSender> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _secrets = secrets;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var cfg = await _config.GetAsync(ct);
        var token = await GetTokenAsync(cfg, ct);

        var payload = new
        {
            message = new
            {
                subject = message.Subject,
                body = new { contentType = "HTML", content = message.HtmlBody },
                from = new { emailAddress = new { address = cfg.GraphSender, name = message.FromName } },
                toRecipients = SplitAddresses(message.To).Select(Recipient).ToArray(),
                ccRecipients = (message.Cc ?? Array.Empty<string>()).Select(Recipient).ToArray(),
                replyTo = string.IsNullOrWhiteSpace(message.ReplyTo)
                    ? Array.Empty<object>()
                    : new[] { Recipient(message.ReplyTo!) },
            },
            saveToSentItems = false,
        };

        var client = _httpFactory.CreateClient();
        var url = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(cfg.GraphSender)}/sendMail";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(payload);

        var resp = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Graph sendMail failed ({Status}): {Body}", (int)resp.StatusCode, body);
            throw new InvalidOperationException($"Graph sendMail failed with {(int)resp.StatusCode}.");
        }

        _logger.LogInformation("Email sent via Graph to {To} (from {Sender}).", message.To, cfg.GraphSender);
    }

    private static object Recipient(string address) => new { emailAddress = new { address } };

    /// <summary>Splits a recipient string on commas/semicolons so a form can notify several addresses.</summary>
    private static IEnumerable<string> SplitAddresses(string raw) =>
        raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private async Task<string> GetTokenAsync(EmailConfiguration cfg, CancellationToken ct)
    {
        if (_token is not null && _tokenForClientId == cfg.GraphClientId && DateTimeOffset.UtcNow < _tokenExpiresAt)
            return _token;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_token is not null && _tokenForClientId == cfg.GraphClientId && DateTimeOffset.UtcNow < _tokenExpiresAt)
                return _token;

            var client = _httpFactory.CreateClient();
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = cfg.GraphClientId,
                ["client_secret"] = _secrets.Unprotect(cfg.GraphClientSecret),
                ["scope"] = "https://graph.microsoft.com/.default",
                ["grant_type"] = "client_credentials",
            });

            var url = $"https://login.microsoftonline.com/{cfg.GraphTenantId}/oauth2/v2.0/token";
            var resp = await client.PostAsync(url, form, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Graph token request failed ({Status}): {Body}", (int)resp.StatusCode, json);
                throw new InvalidOperationException("Failed to acquire Graph token.");
            }

            var tok = JsonSerializer.Deserialize<TokenResponse>(json)!;
            _token = tok.AccessToken;
            _tokenForClientId = cfg.GraphClientId;
            // Refresh a minute early to avoid edge-of-expiry failures.
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tok.ExpiresIn - 60);
            return _token!;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
