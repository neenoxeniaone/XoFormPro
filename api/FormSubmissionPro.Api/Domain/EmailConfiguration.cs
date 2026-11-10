namespace FormSubmissionPro.Api.Domain;

/// <summary>
/// Platform-wide email settings (single row, Id = 1). When the Graph fields are filled,
/// mail is sent via Microsoft Graph; otherwise it falls back to the console dev sink.
/// Editable directly in the database (or later via an admin UI) without redeploying.
/// </summary>
public class EmailConfiguration
{
    public int Id { get; set; } = 1;

    public string DefaultFromName { get; set; } = "XOFormPro";

    /// <summary>Which transport to use: "graph" (default) or "ses".</summary>
    public string Provider { get; set; } = "graph";

    // ── Credential columns (shared by both providers) ──
    // Microsoft Graph (client-credentials, Mail.Send application permission):
    //   GraphTenantId / GraphClientId / GraphClientSecret / GraphSender
    // Amazon SES reuses the SAME four columns with SES meanings:
    //   GraphTenantId     → AWS region            (e.g. eu-west-1)
    //   GraphClientId     → AWS access key id
    //   GraphClientSecret → AWS secret access key (encrypted)
    //   GraphSender       → verified From address (e.g. noreply@gemhotels.com)
    public string GraphTenantId { get; set; } = string.Empty;
    public string GraphClientId { get; set; } = string.Empty;
    public string GraphClientSecret { get; set; } = string.Empty;

    /// <summary>Graph: the mailbox to send as. SES: the verified From address.</summary>
    public string GraphSender { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public bool GraphIsConfigured =>
        !string.IsNullOrWhiteSpace(GraphTenantId) &&
        !string.IsNullOrWhiteSpace(GraphClientId) &&
        !string.IsNullOrWhiteSpace(GraphClientSecret) &&
        !string.IsNullOrWhiteSpace(GraphSender);

    public bool UsesSes => string.Equals(Provider, "ses", StringComparison.OrdinalIgnoreCase);

    /// <summary>SES needs the same four credential columns filled (region, key, secret, From).</summary>
    public bool SesIsConfigured => GraphIsConfigured;
}
