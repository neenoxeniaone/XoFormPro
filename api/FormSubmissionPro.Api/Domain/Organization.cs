namespace FormSubmissionPro.Api.Domain;

/// <summary>
/// A tenant. Every form and submission belongs to exactly one organization.
/// Auth/billing are deferred — for now an organization is created via the dev API
/// and identified in the dashboard by a stubbed context.
/// </summary>
public class Organization
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Default inbox that form submissions are emailed to.</summary>
    public string NotificationEmail { get; set; } = string.Empty;

    /// <summary>free | pro — gates features like file uploads, webhooks, autoresponder.</summary>
    public string PlanTier { get; set; } = "free";

    /// <summary>Monthly submission allowance for the current plan.</summary>
    public int MonthlyQuota { get; set; } = 250;

    /// <summary>
    /// Workspace-wide allow-list (comma-separated domains). When set, EVERY form and list
    /// only accepts submissions whose Origin/Referer matches. Empty = no global restriction.
    /// </summary>
    public string? AllowedDomains { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Form> Forms { get; set; } = new();
}
