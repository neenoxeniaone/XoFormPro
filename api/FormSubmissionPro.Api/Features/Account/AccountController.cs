using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Domain;
using FormSubmissionPro.Api.Infrastructure;
using FormSubmissionPro.Api.Services.Auth;
using FormSubmissionPro.Api.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Features.Account;

public record UpdateAccountRequest(string WorkspaceName, string NotificationEmail, string? AllowedDomains);
public record ChangePlanRequest(string PlanTier);
public record InviteRequest(string Email, string Role);

/// <summary>The current workspace's profile + plan + team. Requires authentication.</summary>
[Authorize]
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly IConfiguration _config;

    public AccountController(AppDbContext db, IEmailSender email, IConfiguration config)
    {
        _db = db;
        _email = email;
        _config = config;
    }

    private string BaseUrl => _config["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173";

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        var org = await _db.Organizations.FindAsync([orgId], ct);
        if (org is null) return NotFound();

        var usage = await UsageThisMonth(orgId, ct);
        return Ok(new
        {
            org.Id, org.Name, org.NotificationEmail, org.PlanTier, org.MonthlyQuota,
            org.AllowedDomains,
            usageThisMonth = usage,
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateAccountRequest req, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        var org = await _db.Organizations.FindAsync([orgId], ct);
        if (org is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.WorkspaceName)) org.Name = req.WorkspaceName.Trim();
        if (!string.IsNullOrWhiteSpace(req.NotificationEmail) && req.NotificationEmail.Contains('@'))
            org.NotificationEmail = req.NotificationEmail.Trim();
        // Empty string clears the global allow-list (no restriction).
        org.AllowedDomains = string.IsNullOrWhiteSpace(req.AllowedDomains) ? null : req.AllowedDomains.Trim();
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>Dev billing stub: switch plan (free/pro) and set the matching quota.</summary>
    [HttpPost("plan")]
    public async Task<IActionResult> ChangePlan(ChangePlanRequest req, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        var org = await _db.Organizations.FindAsync([orgId], ct);
        if (org is null) return NotFound();

        org.PlanTier = req.PlanTier.Equals("pro", StringComparison.OrdinalIgnoreCase) ? "pro" : "free";
        org.MonthlyQuota = org.PlanTier == "pro" ? 25000 : 250;
        await _db.SaveChangesAsync(ct);

        return Ok(new { org.PlanTier, org.MonthlyQuota });
    }

    // ── Team members ──
    [HttpGet("members")]
    public async Task<IActionResult> Members(CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();

        var members = await _db.Users
            .Where(u => u.OrganizationId == orgId)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new { u.Id, u.Email, u.Role, u.EmailVerified, u.CreatedAt })
            .ToListAsync(ct);

        var pending = await _db.Invitations
            .Where(i => i.OrganizationId == orgId && i.AcceptedAt == null && i.ExpiresAt > DateTimeOffset.UtcNow)
            .Select(i => new { i.Id, i.Email, i.Role, i.CreatedAt })
            .ToListAsync(ct);

        return Ok(new { members, pending });
    }

    [HttpPost("invite")]
    public async Task<IActionResult> Invite(InviteRequest req, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        if (!await IsOwner(orgId, ct)) return Forbid();

        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest(new { message = "A valid email is required." });
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new { message = "That person already has an account." });

        var (raw, hash) = SecureTokens.Generate();
        _db.Invitations.Add(new Invitation
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, Email = email,
            Role = req.Role == "owner" ? "owner" : "member",
            TokenHash = hash, ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        });
        await _db.SaveChangesAsync(ct);

        var link = $"{BaseUrl}/accept-invite?token={raw}";
        await _email.SendAsync(new EmailMessage(
            To: email,
            Subject: "You've been invited to a workspace",
            HtmlBody: $"<p>You've been invited to join a FormSubmissionPro workspace. Accept using the link below (valid 7 days):</p><p><a href=\"{link}\">{link}</a></p>"), ct);

        return Ok(new { message = "Invitation sent." });
    }

    [HttpDelete("members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid userId, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        if (!await IsOwner(orgId, ct)) return Forbid();
        if (this.GetUserId() == userId) return BadRequest(new { message = "You can't remove yourself." });

        var member = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, ct);
        if (member is null) return NotFound();

        _db.Users.Remove(member);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("invites/{id:guid}")]
    public async Task<IActionResult> CancelInvite(Guid id, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        if (!await IsOwner(orgId, ct)) return Forbid();

        var invite = await _db.Invitations.FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == orgId, ct);
        if (invite is null) return NotFound();
        _db.Invitations.Remove(invite);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<bool> IsOwner(Guid orgId, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return userId is not null &&
            await _db.Users.AnyAsync(u => u.Id == userId && u.OrganizationId == orgId && u.Role == "owner", ct);
    }

    private async Task<int> UsageThisMonth(Guid orgId, CancellationToken ct)
    {
        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return await _db.Submissions.CountAsync(
            s => s.OrganizationId == orgId && !s.IsSpam && s.CreatedAt >= monthStart, ct);
    }
}
