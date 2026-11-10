namespace FormSubmissionPro.Api.Domain;

/// <summary>
/// A secret API key for the Submissions REST API (Pro). Format: w3f_live_{random}.
/// The full key is shown once at creation; only its hash is stored.
/// </summary>
public class ApiKey
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    /// <summary>Human label, e.g. "Production backend".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>First chars of the key for display, e.g. "w3f_live_a1b2…".</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the full key.</summary>
    public string KeyHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>When set, the key is revoked and rejected.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null;
}
