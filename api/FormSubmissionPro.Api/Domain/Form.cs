namespace FormSubmissionPro.Api.Domain;

/// <summary>
/// A form endpoint. Its <see cref="AccessKey"/> is the PUBLIC identifier embedded in
/// client-side HTML — it is an alias to the destination inbox, not a secret API key
/// (mirrors Web3Forms' access-key model).
/// </summary>
public class Form
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    /// <summary>Public access key sent in the form body / URL. Safe to expose client-side.</summary>
    public Guid AccessKey { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "My Form";

    /// <summary>form | list. A "list" is an email-subscription list (newsletter/waitlist).</summary>
    public string Kind { get; set; } = "form";

    /// <summary>Send the owner a notification email per submission. Lists default this off.</summary>
    public bool NotifyByEmail { get; set; } = true;

    /// <summary>Optional per-form recipient override; falls back to the org's NotificationEmail.</summary>
    public string? ToEmailOverride { get; set; }

    /// <summary>Default email subject when the submission omits a `subject` field.</summary>
    public string DefaultSubject { get; set; } = "New Form Submission";

    /// <summary>Default "from name" shown on the notification email.</summary>
    public string FromName { get; set; } = "XOFormPro";

    /// <summary>Optional default success redirect URL.</summary>
    public string? RedirectUrl { get; set; }

    /// <summary>When false, submissions are rejected.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Honeypot is always on; this toggles captcha verification (hCaptcha/Turnstile).</summary>
    public bool RequireCaptcha { get; set; } = false;

    /// <summary>none | hcaptcha | turnstile.</summary>
    public string CaptchaProvider { get; set; } = "none";

    /// <summary>Provider secret key used for server-side verification.</summary>
    public string? CaptchaSecret { get; set; }

    /// <summary>Provider public site key — safe to expose; rendered in the form's captcha widget.</summary>
    public string? CaptchaSiteKey { get; set; }

    /// <summary>Comma-separated allow-list of domains permitted to submit (Pro). Empty = any.</summary>
    public string? AllowedDomains { get; set; }

    // ── Autoresponder (Pro): sends a confirmation email back to the submitter ──
    public bool AutoresponderEnabled { get; set; } = false;
    public string? AutoresponderSubject { get; set; }
    public string? AutoresponderBody { get; set; }

    // ── Per-form notification email + success page customization ──
    /// <summary>Optional intro line at the top of the notification email.</summary>
    public string? EmailIntro { get; set; }
    /// <summary>Heading shown on the default success page.</summary>
    public string? SuccessTitle { get; set; }
    /// <summary>Body text shown on the default success page.</summary>
    public string? SuccessMessage { get; set; }

    // ── Bot checks that need forms.js ─────────────────────────────────────────
    // Both are opt-in per key: a form that posts natively (because forms.js failed to
    // load) carries neither, so switching them on for a key that needs the fallback
    // would reject real submissions.

    /// <summary>
    /// Show an "I'm not a robot" tick box, rendered by forms.js on the embedding site.
    /// Ticking it is what causes forms.js to request a form token, so the gesture is
    /// what produces the proof — a direct POST cannot fake it.
    /// </summary>
    public bool RequireHumanCheck { get; set; } = false;

    /// <summary>
    /// Require a signed, single-use token issued by /form-token at page load. Set
    /// automatically alongside <see cref="RequireHumanCheck"/>; can also stand alone.
    /// </summary>
    public bool RequireToken { get; set; } = false;

    /// <summary>
    /// Let forms.js inject the hCaptcha/Turnstile widget itself, so the embedding site
    /// needs no markup change. Requires <see cref="RequireCaptcha"/> and a site key.
    /// </summary>
    public bool CaptchaAutoInject { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Submission> Submissions { get; set; } = new();
    public List<Webhook> Webhooks { get; set; } = new();
}
