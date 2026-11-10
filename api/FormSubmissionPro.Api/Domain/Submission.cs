using System.Text.Json;

namespace FormSubmissionPro.Api.Domain;

/// <summary>
/// One captured form submission. All non-reserved fields are stored verbatim as JSON
/// so arbitrary forms work without schema changes.
/// </summary>
public class Submission
{
    public Guid Id { get; set; }

    public Guid FormId { get; set; }
    public Form? Form { get; set; }

    /// <summary>Denormalized for tenant-scoped queries and quota counting.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>All submitted fields (minus reserved ones) as a JSON object — stored as jsonb.</summary>
    public JsonDocument Data { get; set; } = JsonDocument.Parse("{}");

    /// <summary>The submitter's email, if a `email` field was provided (used for reply-to).</summary>
    public string? SenderEmail { get; set; }

    public string Subject { get; set; } = string.Empty;

    /// <summary>Denormalized concatenation of submitted values, for fast text search (ILIKE).</summary>
    public string SearchText { get; set; } = string.Empty;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>Flagged by honeypot/captcha; spam is stored but not emailed.</summary>
    public bool IsSpam { get; set; }

    /// <summary>Marked read in the dashboard.</summary>
    public bool IsRead { get; set; }

    /// <summary>Starred/flagged by the user for follow-up.</summary>
    public bool IsStarred { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
