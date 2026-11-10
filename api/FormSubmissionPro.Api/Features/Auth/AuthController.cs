using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Domain;
using FormSubmissionPro.Api.Infrastructure;
using FormSubmissionPro.Api.Services.Auth;
using FormSubmissionPro.Api.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Features.Auth;

public record RegisterRequest(string Email, string Password, string? WorkspaceName);
public record LoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record TokenRequest(string Token);
public record AcceptInviteRequest(string Token, string Password);

[ApiController]
[Route("api/auth")]
// Throttled per IP: login has no lockout, and forgot-password will mail anyone.
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IEmailSender _email;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IJwtTokenService jwt, IEmailSender email, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _email = email;
        _config = config;
    }

    private string BaseUrl => _config["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173";

    /// <summary>Creates a workspace (organization) + first user, returns a JWT.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest(new { message = "A valid email is required." });
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 5)
            return BadRequest(new { message = "Password must be at least 5 characters." });
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new { message = "An account with this email already exists." });

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(req.WorkspaceName) ? email : req.WorkspaceName.Trim(),
            NotificationEmail = email,
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Email = email,
            PasswordHash = PasswordHasher.Hash(req.Password),
        };
        _db.Organizations.Add(org);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await SendVerificationEmailAsync(user, ct);

        return Ok(new { token = _jwt.CreateToken(user), user = Shape(user, org) });
    }

    /// <summary>Issues a password-reset link by email. Always returns 200 (no account enumeration).</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req, CancellationToken ct)
    {
        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is not null)
        {
            var (raw, hash) = SecureTokens.Generate();
            _db.UserTokens.Add(new UserToken
            {
                Id = Guid.NewGuid(), UserId = user.Id, Purpose = UserTokenPurpose.PasswordReset,
                TokenHash = hash, ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            });
            await _db.SaveChangesAsync(ct);

            var link = $"{BaseUrl}/reset-password?token={raw}";
            await _email.SendAsync(new EmailMessage(
                To: user.Email,
                Subject: "Reset your password",
                HtmlBody: $"<p>Reset your XOFormPro password using the link below (valid for 1 hour):</p><p><a href=\"{link}\">{link}</a></p>"), ct);
        }
        return Ok(new { message = "If that email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 5)
            return BadRequest(new { message = "Password must be at least 5 characters." });

        var token = await ConsumeTokenAsync(req.Token, UserTokenPurpose.PasswordReset, ct);
        if (token is null) return BadRequest(new { message = "Invalid or expired reset link." });

        var user = await _db.Users.FindAsync([token.UserId], ct);
        if (user is null) return BadRequest(new { message = "Invalid reset link." });

        user.PasswordHash = PasswordHasher.Hash(req.NewPassword);
        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Password updated. You can now sign in." });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(TokenRequest req, CancellationToken ct)
    {
        var token = await ConsumeTokenAsync(req.Token, UserTokenPurpose.EmailVerification, ct);
        if (token is null) return BadRequest(new { message = "Invalid or expired verification link." });

        var user = await _db.Users.FindAsync([token.UserId], ct);
        if (user is null) return BadRequest(new { message = "Invalid verification link." });

        user.EmailVerified = true;
        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Email verified." });
    }

    /// <summary>Accepts a team invitation: creates a member user in the inviting workspace.</summary>
    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvite(AcceptInviteRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 5)
            return BadRequest(new { message = "Password must be at least 5 characters." });

        var hash = SecureTokens.Hash(req.Token ?? "");
        var invite = await _db.Invitations.FirstOrDefaultAsync(i => i.TokenHash == hash && i.AcceptedAt == null, ct);
        if (invite is null || invite.ExpiresAt < DateTimeOffset.UtcNow)
            return BadRequest(new { message = "Invalid or expired invitation." });
        if (await _db.Users.AnyAsync(u => u.Email == invite.Email, ct))
            return Conflict(new { message = "An account with this email already exists." });

        var org = await _db.Organizations.FindAsync([invite.OrganizationId], ct);
        if (org is null) return BadRequest(new { message = "Workspace no longer exists." });

        var user = new User
        {
            Id = Guid.NewGuid(), OrganizationId = invite.OrganizationId, Email = invite.Email,
            PasswordHash = PasswordHasher.Hash(req.Password), Role = invite.Role, EmailVerified = true,
        };
        invite.AcceptedAt = DateTimeOffset.UtcNow;
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new { token = _jwt.CreateToken(user), user = Shape(user, org) });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        var user = await _db.Users.Include(u => u.Organization).FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(new { token = _jwt.CreateToken(user), user = Shape(user, user.Organization!) });
    }

    /// <summary>Returns the current authenticated user + workspace.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = this.GetUserId();
        if (userId is null) return Unauthorized();
        var user = await _db.Users.Include(u => u.Organization).FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Unauthorized();
        return Ok(Shape(user, user.Organization!));
    }

    /// <summary>Marks a token used and returns it if valid + unexpired.</summary>
    private async Task<UserToken?> ConsumeTokenAsync(string? raw, UserTokenPurpose purpose, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var hash = SecureTokens.Hash(raw);
        var token = await _db.UserTokens.FirstOrDefaultAsync(
            t => t.TokenHash == hash && t.Purpose == purpose && t.UsedAt == null, ct);
        if (token is null || token.ExpiresAt < DateTimeOffset.UtcNow) return null;
        token.UsedAt = DateTimeOffset.UtcNow;
        return token;
    }

    private async Task SendVerificationEmailAsync(User user, CancellationToken ct)
    {
        var (raw, hash) = SecureTokens.Generate();
        _db.UserTokens.Add(new UserToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, Purpose = UserTokenPurpose.EmailVerification,
            TokenHash = hash, ExpiresAt = DateTimeOffset.UtcNow.AddDays(3),
        });
        await _db.SaveChangesAsync(ct);

        var link = $"{BaseUrl}/verify-email?token={raw}";
        await _email.SendAsync(new EmailMessage(
            To: user.Email,
            Subject: "Verify your email",
            HtmlBody: $"<p>Welcome to XOFormPro! Confirm your email using the link below:</p><p><a href=\"{link}\">{link}</a></p>"), ct);
    }

    private static object Shape(User u, Organization o) => new
    {
        userId = u.Id,
        email = u.Email,
        role = u.Role,
        emailVerified = u.EmailVerified,
        organizationId = o.Id,
        workspaceName = o.Name,
        notificationEmail = o.NotificationEmail,
        planTier = o.PlanTier,
        monthlyQuota = o.MonthlyQuota,
    };
}
