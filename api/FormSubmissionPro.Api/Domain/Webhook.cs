namespace FormSubmissionPro.Api.Domain;

/// <summary>
/// An outbound webhook. On each (non-spam) submission the full payload is POSTed as JSON
/// to <see cref="Url"/>. Targets Zapier/Make/n8n/Slack/Discord/Google Sheets, etc.
/// </summary>
public class Webhook
{
    public Guid Id { get; set; }

    public Guid FormId { get; set; }
    public Form? Form { get; set; }

    public Guid OrganizationId { get; set; }

    public string Url { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
