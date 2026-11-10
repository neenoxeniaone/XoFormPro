using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Services.Spam;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Features.Submit;

/// <summary>
/// The two public endpoints forms.js calls on page load.
///
/// Both are deliberately unauthenticated and keyed by the same public access key that is
/// already in the page source: they return only what is safe to embed client-side (the
/// captcha provider and its *site* key), and a token bound to the requesting origin.
/// Neither reveals whether a key exists — an unknown key gets the same inert config an
/// unprotected one does.
/// </summary>
[ApiController]
[EnableRateLimiting("submit")]
public class FormConfigController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFormTokenService _tokens;

    public FormConfigController(AppDbContext db, IFormTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    /// <summary>
    /// What forms.js needs to enhance a form without the embedding site changing any markup:
    /// whether to inject a captcha widget, and whether to fetch a token.
    /// </summary>
    [HttpGet("/form-config")]
    public async Task<IActionResult> Config([FromQuery] string? key, CancellationToken ct)
    {
        // Cached briefly: this is hit on every page load of every embedding site.
        Response.Headers.CacheControl = "public, max-age=60";

        if (!Guid.TryParse(key, out var accessKey))
            return Ok(Inert);

        var form = await _db.Forms
            .AsNoTracking()
            .Where(f => f.AccessKey == accessKey && f.IsActive)
            .Select(f => new
            {
                f.RequireCaptcha,
                f.CaptchaAutoInject,
                f.CaptchaProvider,
                f.CaptchaSiteKey,
                f.RequireHumanCheck,
                f.RequireToken,
            })
            .FirstOrDefaultAsync(ct);

        if (form is null) return Ok(Inert);

        // Only advertise the widget when it can actually render: auto-inject on, a real
        // provider, and a site key present. Otherwise the site's own markup is in charge.
        var injectCaptcha = form.RequireCaptcha
                            && form.CaptchaAutoInject
                            && !string.IsNullOrWhiteSpace(form.CaptchaSiteKey)
                            && form.CaptchaProvider is "hcaptcha" or "turnstile";

        return Ok(new
        {
            captcha = injectCaptcha
                ? new { provider = form.CaptchaProvider, siteKey = form.CaptchaSiteKey }
                : null,
            humanCheck = form.RequireHumanCheck,
            requireToken = form.RequireToken,
        });
    }

    /// <summary>
    /// Issues a single-use token proving a real page load. forms.js fetches one on load and
    /// attaches it as a hidden `_xfp_token` field.
    /// </summary>
    [HttpGet("/form-token")]
    public async Task<IActionResult> Token([FromQuery] string? key, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";

        if (!Guid.TryParse(key, out var accessKey))
            return BadRequest(new { message = "Invalid or missing key." });

        var form = await _db.Forms.FirstOrDefaultAsync(f => f.AccessKey == accessKey && f.IsActive, ct);
        if (form is null)
            return NotFound(new { message = "Access key not found." });

        var origin = OriginMatcher.HostOf(Request.Headers.Origin.ToString(), Request.Headers.Referer.ToString());
        var issued = await _tokens.IssueAsync(form, origin, ct);

        return Ok(new { token = issued.Token, expiresAt = issued.ExpiresAt });
    }

    /// <summary>The response for an unknown or inactive key — indistinguishable from a plain one.</summary>
    private static object Inert => new { captcha = (object?)null, humanCheck = false, requireToken = false };
}
