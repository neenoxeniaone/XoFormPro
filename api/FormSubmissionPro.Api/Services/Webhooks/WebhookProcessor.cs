using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Services.Webhooks;

/// <summary>
/// Polls the WebhookDeliveries outbox and delivers due records with exponential backoff.
/// Durable across restarts (replaces the old in-memory channel).
/// </summary>
public class WebhookProcessor : BackgroundService
{
    private const int MaxAttempts = 6;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    // Backoff per attempt number (1-based): 30s, 2m, 10m, 1h, 6h.
    private static readonly TimeSpan[] Backoff =
    {
        TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10),
        TimeSpan.FromHours(1), TimeSpan.FromHours(6),
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<WebhookProcessor> _logger;

    public WebhookProcessor(IServiceScopeFactory scopeFactory, IHttpClientFactory httpFactory, ILogger<WebhookProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook processor loop error.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ProcessDueAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var due = await db.WebhookDeliveries
            .Where(d => d.Status == WebhookDeliveryStatus.Pending && d.NextAttemptAt <= now)
            .OrderBy(d => d.NextAttemptAt)
            .Take(25)
            .ToListAsync(ct);

        if (due.Count == 0) return;

        var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        foreach (var delivery in due)
        {
            delivery.Attempts++;
            delivery.LastAttemptAt = DateTimeOffset.UtcNow;
            try
            {
                var (body, contentType) = WebhookPayloadFormatter.Format(delivery.Url, delivery.Payload);
                using var content = new StringContent(body, System.Text.Encoding.UTF8, contentType);
                var resp = await client.PostAsync(delivery.Url, content, ct);
                delivery.ResponseStatus = (int)resp.StatusCode;

                if (resp.IsSuccessStatusCode)
                {
                    delivery.Status = WebhookDeliveryStatus.Succeeded;
                    delivery.Error = null;
                }
                else
                {
                    Reschedule(delivery, $"HTTP {(int)resp.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                delivery.ResponseStatus = null;
                Reschedule(delivery, ex.Message);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private void Reschedule(WebhookDelivery d, string error)
    {
        d.Error = error;
        if (d.Attempts >= MaxAttempts)
        {
            d.Status = WebhookDeliveryStatus.Failed;
            _logger.LogWarning("Webhook {Id} → {Url} permanently failed after {Attempts} attempts: {Error}",
                d.Id, d.Url, d.Attempts, error);
        }
        else
        {
            var delay = Backoff[Math.Min(d.Attempts - 1, Backoff.Length - 1)];
            d.NextAttemptAt = DateTimeOffset.UtcNow.Add(delay);
        }
    }
}
