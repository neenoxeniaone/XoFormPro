namespace FormSubmissionPro.Api.Domain;

public enum WebhookDeliveryStatus { Pending = 0, Succeeded = 1, Failed = 2 }

/// <summary>
/// A durable outbox record for one webhook delivery. Survives restarts and is retried
/// with backoff until it succeeds or exhausts its attempts.
/// </summary>
public class WebhookDelivery
{
    public Guid Id { get; set; }

    public Guid WebhookId { get; set; }
    public Guid FormId { get; set; }
    public Guid OrganizationId { get; set; }

    public string Url { get; set; } = string.Empty;

    /// <summary>The JSON body to POST (already serialized).</summary>
    public string Payload { get; set; } = "{}";

    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;
    public int Attempts { get; set; }
    public int? ResponseStatus { get; set; }
    public string? Error { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastAttemptAt { get; set; }
}
