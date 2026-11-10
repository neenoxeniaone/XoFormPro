using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Domain;
using FormSubmissionPro.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Features.Forms;

public record CreateFormRequest(string Name, string? ToEmailOverride, string? DefaultSubject, string? RedirectUrl, string? Kind);
public record UpdateFormRequest(
    string Name, string? ToEmailOverride, string? DefaultSubject, string? FromName,
    string? RedirectUrl, bool IsActive, bool RequireCaptcha, string? AllowedDomains,
    string? CaptchaProvider, string? CaptchaSecret, string? CaptchaSiteKey,
    bool AutoresponderEnabled, string? AutoresponderSubject, string? AutoresponderBody,
    string? EmailIntro, string? SuccessTitle, string? SuccessMessage, bool NotifyByEmail = true,
    // Bot checks. Optional so a caller that predates them cannot silently reset them.
    bool? RequireHumanCheck = null, bool? RequireToken = null, bool? CaptchaAutoInject = null);

/// <summary>Tenant-scoped CRUD for forms (access keys). Requires authentication.</summary>
[Authorize]
[ApiController]
[Route("api/forms")]
public class FormsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly FormSubmissionPro.Api.Services.Security.ISecretProtector _secrets;
    public FormsController(AppDbContext db, FormSubmissionPro.Api.Services.Security.ISecretProtector secrets)
    {
        _db = db;
        _secrets = secrets;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string kind = "form", CancellationToken ct = default)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();

        var forms = await _db.Forms
            .Where(f => f.OrganizationId == orgId && f.Kind == kind)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id, f.AccessKey, f.Name, f.Kind, f.NotifyByEmail, f.ToEmailOverride, f.DefaultSubject, f.FromName,
                f.RedirectUrl, f.IsActive, f.RequireCaptcha, f.AllowedDomains, f.CreatedAt,
                f.CaptchaProvider, f.CaptchaSiteKey, f.AutoresponderEnabled, f.AutoresponderSubject, f.AutoresponderBody,
                f.EmailIntro, f.SuccessTitle, f.SuccessMessage,
                f.RequireHumanCheck, f.RequireToken, f.CaptchaAutoInject,
                SubmissionCount = f.Submissions.Count(s => !s.IsSpam)
            })
            .ToListAsync(ct);

        return Ok(forms);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFormRequest req, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        if (!await _db.Organizations.AnyAsync(o => o.Id == orgId, ct)) return Unauthorized();

        var isList = req.Kind == "list";
        var kind = isList ? "list" : "form";
        var name = string.IsNullOrWhiteSpace(req.Name) ? (isList ? "My List" : "My Form") : req.Name.Trim();

        // Names must be unique per workspace within the same kind (case-insensitive).
        var lower = name.ToLower();
        if (await _db.Forms.AnyAsync(f => f.OrganizationId == orgId && f.Kind == kind && f.Name.ToLower() == lower, ct))
            return Conflict(new { message = $"A {kind} named \"{name}\" already exists." });

        var form = new Form
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Kind = kind,
            Name = name,
            ToEmailOverride = req.ToEmailOverride,
            DefaultSubject = string.IsNullOrWhiteSpace(req.DefaultSubject)
                ? (isList ? "New Subscriber" : "New Form Submission") : req.DefaultSubject,
            RedirectUrl = req.RedirectUrl,
            // Both forms and lists notify the owner by email by default.
            NotifyByEmail = true,
        };
        _db.Forms.Add(form);
        await _db.SaveChangesAsync(ct);

        return Ok(new { form.Id, form.AccessKey, form.Name, form.Kind });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateFormRequest req, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();

        var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == orgId, ct);
        if (form is null) return NotFound();

        // Enforce unique name per workspace + kind (excluding this one).
        var newName = string.IsNullOrWhiteSpace(req.Name) ? form.Name : req.Name.Trim();
        var lower = newName.ToLower();
        if (await _db.Forms.AnyAsync(f => f.OrganizationId == orgId && f.Kind == form.Kind && f.Id != form.Id && f.Name.ToLower() == lower, ct))
            return Conflict(new { message = $"A {form.Kind} named \"{newName}\" already exists." });

        form.Name = newName;
        form.ToEmailOverride = req.ToEmailOverride;
        form.DefaultSubject = req.DefaultSubject ?? form.DefaultSubject;
        form.FromName = req.FromName ?? form.FromName;
        form.RedirectUrl = req.RedirectUrl;
        form.IsActive = req.IsActive;
        form.RequireCaptcha = req.RequireCaptcha;
        form.AllowedDomains = req.AllowedDomains;
        form.CaptchaProvider = string.IsNullOrWhiteSpace(req.CaptchaProvider) ? "none" : req.CaptchaProvider;
        // Only overwrite the secret when a new value is supplied (it's write-only from the UI).
        if (!string.IsNullOrWhiteSpace(req.CaptchaSecret))
            form.CaptchaSecret = _secrets.Protect(req.CaptchaSecret);
        // Site key is public; store as-is (empty → cleared).
        form.CaptchaSiteKey = string.IsNullOrWhiteSpace(req.CaptchaSiteKey) ? null : req.CaptchaSiteKey.Trim();
        form.AutoresponderEnabled = req.AutoresponderEnabled;
        form.AutoresponderSubject = req.AutoresponderSubject;
        form.AutoresponderBody = req.AutoresponderBody;
        form.EmailIntro = req.EmailIntro;
        form.SuccessTitle = req.SuccessTitle;
        form.SuccessMessage = req.SuccessMessage;
        form.NotifyByEmail = req.NotifyByEmail;

        if (req.RequireHumanCheck is { } human) form.RequireHumanCheck = human;
        if (req.CaptchaAutoInject is { } cai) form.CaptchaAutoInject = cai;

        // The tick box is only meaningful when ticking it produces a signed token, so the
        // token check follows it. An explicit RequireToken still wins, which leaves the
        // token usable on its own for anyone who wants the check without a visible box.
        if (req.RequireToken is { } token) form.RequireToken = token;
        else if (req.RequireHumanCheck is { } human2) form.RequireToken = human2;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();

        var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == orgId, ct);
        if (form is null) return NotFound();

        _db.Forms.Remove(form);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
