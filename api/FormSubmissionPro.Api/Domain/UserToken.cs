namespace FormSubmissionPro.Api.Domain;

public enum UserTokenPurpose { PasswordReset = 0, EmailVerification = 1 }

/// <summary>A single-use, time-limited token for password reset or email verification.</summary>
public class UserToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public UserTokenPurpose Purpose { get; set; }

    /// <summary>SHA-256 hash of the raw token (the raw value is only emailed, never stored).</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
