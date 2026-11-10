namespace FormSubmissionPro.Api.Domain;

/// <summary>
/// A single-use nonce issued to a real page load and redeemed by the matching submission.
/// The token handed to the browser is an HMAC over {formId, origin, issuedAt, nonce}; this
/// row exists so the nonce can be burned on use and a captured payload cannot be replayed.
/// </summary>
public class FormToken
{
    /// <summary>The nonce itself.</summary>
    public Guid Id { get; set; }

    public Guid FormId { get; set; }

    /// <summary>Origin the token was issued to; the submission must arrive from the same one.</summary>
    public string Origin { get; set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
