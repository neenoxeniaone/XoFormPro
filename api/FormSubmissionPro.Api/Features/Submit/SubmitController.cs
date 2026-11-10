using System.Text.Json;
using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Domain;
using FormSubmissionPro.Api.Services.Captcha;
using FormSubmissionPro.Api.Services.Email;
using FormSubmissionPro.Api.Services.Files;
using FormSubmissionPro.Api.Services.Spam;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Features.Submit;

/// <summary>
/// Public submission endpoint. Mirrors Web3Forms' contract:
///   POST /submit            (access_key in body)
///   POST /submit/{accessKey}
/// Accepts application/x-www-form-urlencoded, multipart/form-data, or application/json.
/// </summary>
[ApiController]
[EnableRateLimiting("submit")]
public class SubmitController : ControllerBase
{
    // Control fields that are NOT shown in the email body; everything else is forwarded
    // as-is. NOTE: "email" is intentionally NOT reserved — it's the submitter's address,
    // so it must appear in the email (and is also used as Reply-To, read separately).
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "access_key", "subject", "from_name", "replyto",
        "redirect", "botcheck", "ccemail", "h-captcha-response", "cf-turnstile-response",
        "not_robot", "_xfp_token",
    };

    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly ICaptchaVerifier _captcha;
    private readonly IFormTokenService _tokens;
    private readonly IFileStorage _files;
    private readonly Services.Security.ISecretProtector _secrets;
    private readonly ILogger<SubmitController> _logger;

    public SubmitController(
        AppDbContext db,
        IEmailSender email,
        ICaptchaVerifier captcha,
        IFormTokenService tokens,
        IFileStorage files,
        Services.Security.ISecretProtector secrets,
        ILogger<SubmitController> logger)
    {
        _db = db;
        _email = email;
        _captcha = captcha;
        _tokens = tokens;
        _files = files;
        _secrets = secrets;
        _logger = logger;
    }

    [HttpPost("/submit")]
    public Task<IActionResult> Submit(CancellationToken ct) => Handle(null, ct);

    [HttpPost("/submit/{accessKey:guid}")]
    public Task<IActionResult> SubmitByKey(Guid accessKey, CancellationToken ct) => Handle(accessKey, ct);

    private async Task<IActionResult> Handle(Guid? accessKeyFromRoute, CancellationToken ct)
    {
        var fields = await ReadFieldsAsync(ct);

        // Resolve access key from route or body.
        Guid accessKey;
        if (accessKeyFromRoute is { } routeKey)
            accessKey = routeKey;
        else if (fields.TryGetValue("access_key", out var raw) && Guid.TryParse(raw, out var parsed))
            accessKey = parsed;
        else
            return Fail(400, "Invalid or missing access_key.");

        var form = await _db.Forms
            .Include(f => f.Organization)
            .Include(f => f.Webhooks)
            .FirstOrDefaultAsync(f => f.AccessKey == accessKey, ct);

        if (form is null || form.Organization is null)
            return Fail(400, "Access key not found.");
        if (!form.IsActive)
            return Fail(403, "This form is not accepting submissions.");

        // The host the submission came from, parsed once and reused by the checks below.
        var origin = OriginMatcher.HostOf(
            Request.Headers.Origin.ToString(), Request.Headers.Referer.ToString());

        // Domain allow-list: block submissions whose Origin/Referer isn't whitelisted.
        if (!IsOriginAllowed(form, origin))
            return Fail(403, "Submissions from this domain are not allowed.");

        // "I'm not a robot" tick box. On its own a checkbox proves nothing — a direct POST
        // can just send the field — which is why enabling it also turns on the signed token
        // below. forms.js only requests a token once the visitor actually ticks the box, so
        // the two together are what make the gesture meaningful.
        if (form.RequireHumanCheck && !IsTicked(fields.GetValueOrDefault("not_robot")))
            return Fail(403, "Please confirm you're not a robot and try again.");

        // Signed form token: proves the submission followed a real page load.
        if (form.RequireToken)
        {
            var result = await _tokens.VerifyAsync(form, fields.GetValueOrDefault("_xfp_token"), origin, ct);
            if (!result.Valid)
            {
                _logger.LogInformation("Form {FormId} rejected a token: {Reason}", form.Id, result.Reason);
                return Fail(403, "Form session invalid or expired. Please reload the page and try again.");
            }
        }

        // Captcha (Pro): hard-fail if required and the token doesn't verify.
        if (form.RequireCaptcha)
        {
            var token = fields.GetValueOrDefault("h-captcha-response")
                        ?? fields.GetValueOrDefault("cf-turnstile-response");
            var ok = await _captcha.VerifyAsync(
                form.CaptchaProvider, _secrets.Unprotect(form.CaptchaSecret), token,
                HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            if (!ok) return Fail(403, "Captcha verification failed.");
        }

        // Honeypot: legit users never fill the hidden `botcheck` field.
        var isSpam = fields.TryGetValue("botcheck", out var bot) && !string.IsNullOrWhiteSpace(bot);

        // Reserved values.
        fields.TryGetValue("subject", out var subject);
        fields.TryGetValue("from_name", out var fromName);
        fields.TryGetValue("email", out var senderEmail);
        fields.TryGetValue("replyto", out var replyTo);
        fields.TryGetValue("redirect", out var redirect);
        fields.TryGetValue("ccemail", out var ccEmail);

        // The form's configured Default subject is the email subject. A `subject` field
        // posted by the form is only a fallback (used if the form somehow has no default).
        subject = !string.IsNullOrWhiteSpace(form.DefaultSubject) ? form.DefaultSubject
            : !string.IsNullOrWhiteSpace(subject) ? subject
            : "New Form Submission";

        // Everything that isn't reserved becomes the visible payload.
        var payload = fields
            .Where(kv => !Reserved.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var submissionId = Guid.NewGuid();
        var isPro = string.Equals(form.Organization.PlanTier, "pro", StringComparison.OrdinalIgnoreCase);

        // File uploads (Pro): persist each file and surface its URL in the payload.
        if (isPro && Request.HasFormContentType && Request.Form.Files.Count > 0)
        {
            foreach (var file in Request.Form.Files)
            {
                if (file.Length == 0) continue;
                var stored = await _files.SaveAsync(submissionId, file.Name, file, ct);
                payload[file.Name] = stored.Url;
            }
        }

        var submission = new Submission
        {
            Id = submissionId,
            FormId = form.Id,
            OrganizationId = form.OrganizationId,
            Data = JsonSerializer.SerializeToDocument(payload),
            SenderEmail = senderEmail,
            Subject = subject,
            SearchText = string.Join(" ", payload.Values),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            IsSpam = isSpam,
        };
        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync(ct);

        if (!isSpam)
        {
            var to = string.IsNullOrWhiteSpace(form.ToEmailOverride)
                ? form.Organization.NotificationEmail
                : form.ToEmailOverride!;

            var cc = string.IsNullOrWhiteSpace(ccEmail)
                ? null
                : ccEmail.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // 1) Notification email to the owner — unless disabled (e.g. for subscription lists).
            // Email failures are logged, not thrown. The submission is already saved, so a 500
            // here would tell the visitor it failed (inviting a duplicate resubmit) and would
            // skip the webhook writes below.
            if (form.NotifyByEmail)
            {
                await TrySendAsync("notification", submission.Id, new EmailMessage(
                    To: to,
                    Subject: subject,
                    // Renderer picks a kind-aware default intro (subscriber vs submission) when none is set.
                    HtmlBody: SubmissionEmailRenderer.Render(payload, form.Name, form.EmailIntro, form.Kind),
                    ReplyTo: !string.IsNullOrWhiteSpace(replyTo) ? replyTo : senderEmail,
                    // Sender display name = the form's name (a posted `from_name` still overrides it).
                    FromName: string.IsNullOrWhiteSpace(fromName) ? form.Name : fromName,
                    Cc: cc));
            }

            // 2) Autoresponder (Pro): confirmation back to the submitter.
            if (isPro && form.AutoresponderEnabled && !string.IsNullOrWhiteSpace(senderEmail))
            {
                await TrySendAsync("autoresponder", submission.Id, new EmailMessage(
                    To: senderEmail!,
                    Subject: string.IsNullOrWhiteSpace(form.AutoresponderSubject)
                        ? "Thanks for your submission" : form.AutoresponderSubject!,
                    HtmlBody: $"<div style=\"font-family:Arial,sans-serif\">{form.AutoresponderBody ?? "We received your message and will get back to you soon."}</div>",
                    FromName: form.Name));
            }

            // 3) Webhooks (Pro): write durable outbox records; the processor delivers them.
            if (isPro)
            {
                var activeHooks = form.Webhooks.Where(w => w.IsActive).ToList();
                if (activeHooks.Count > 0)
                {
                    var body = JsonSerializer.Serialize(new
                    {
                        formId = form.Id,
                        formName = form.Name,
                        submissionId = submission.Id,
                        submittedAt = submission.CreatedAt,
                        data = payload,
                    });
                    foreach (var hook in activeHooks)
                    {
                        _db.WebhookDeliveries.Add(new Domain.WebhookDelivery
                        {
                            Id = Guid.NewGuid(),
                            WebhookId = hook.Id,
                            FormId = form.Id,
                            OrganizationId = form.OrganizationId,
                            Url = hook.Url,
                            Payload = body,
                        });
                    }
                    await _db.SaveChangesAsync(ct);
                }
            }
        }
        else
        {
            _logger.LogInformation("Submission {Id} flagged as spam (honeypot).", submission.Id);
        }

        // Resolve a redirect target: per-request `redirect` field overrides the form's setting.
        var redirectTarget = !string.IsNullOrWhiteSpace(redirect) ? redirect : form.RedirectUrl;
        var successMessage = string.IsNullOrWhiteSpace(form.SuccessMessage)
            ? "Form submitted successfully"
            : form.SuccessMessage!;

        // Plain browser form posts (no JavaScript) must navigate somewhere:
        //   - redirect if a success URL is configured, otherwise show the hosted success page.
        if (WantsHtml())
        {
            return !string.IsNullOrWhiteSpace(redirectTarget)
                ? Redirect(redirectTarget)
                : Content(SuccessPage.Html(form.SuccessTitle, form.SuccessMessage), "text/html");
        }

        // AJAX / API callers never get a server-side redirect — they stay on their own page
        // and handle success client-side (toast, data-success element, etc.). The redirect URL
        // is returned only when one is configured, so the page can choose to navigate.
        return Ok(new
        {
            success = true,
            message = successMessage,
            data = payload,
            redirect = string.IsNullOrWhiteSpace(redirectTarget) ? null : redirectTarget,
        });
    }

    /// <summary>Flattens form-encoded, multipart, or JSON body into string fields.</summary>
    private async Task<Dictionary<string, string>> ReadFieldsAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(ct);
            foreach (var kv in form)
                result[kv.Key] = string.Join(", ", kv.Value.ToArray());
            return result;
        }

        if (Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                    result[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString() ?? string.Empty
                        : prop.Value.GetRawText();
            }
        }

        return result;
    }

    /// <summary>
    /// Sends an email without letting a transport failure fail the request. Not tied to the
    /// request's cancellation token: a visitor closing the tab must not abort the owner's mail.
    /// </summary>
    private async Task TrySendAsync(string kind, Guid submissionId, EmailMessage message)
    {
        try
        {
            await _email.SendAsync(message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {Kind} email for submission {Id} to {To}.",
                kind, submissionId, message.To);
        }
    }

    /// <summary>A checkbox arrives as "on" from a form post, or true/1 from JSON.</summary>
    private static bool IsTicked(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value is not "false" and not "0" and not "off";

    /// <summary>
    /// True when no allow-list is configured, or the request's host matches one.
    ///
    /// The workspace-wide list and the per-form list are merged rather than ANDed: gating
    /// on both independently meant setting a workspace list silently broke every form that
    /// had its own, narrower one.
    /// </summary>
    private static bool IsOriginAllowed(Form form, string? origin)
    {
        var org = form.Organization?.AllowedDomains;
        var combined = string.IsNullOrWhiteSpace(org)
            ? form.AllowedDomains ?? ""
            : string.IsNullOrWhiteSpace(form.AllowedDomains) ? org : org + "," + form.AllowedDomains;

        var allowed = OriginMatcher.ParseList(combined);
        if (allowed.Length == 0) return true; // no restriction

        // A restriction is set but the request carries no origin at all: fail closed.
        return OriginMatcher.IsAllowed(origin, allowed);
    }

    /// <summary>True when the caller is a browser navigating from a form post (not AJAX/JSON).</summary>
    private bool WantsHtml()
    {
        // AJAX/fetch JSON callers should keep getting JSON.
        if (Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            return false;
        var accept = Request.Headers.Accept.ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult Fail(int status, string message)
        => StatusCode(status, new { success = false, message });
}
