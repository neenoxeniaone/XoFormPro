namespace FormSubmissionPro.Api.Domain;

/// <summary>
/// A login identity. Each user belongs to one organization (their workspace/account).
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash (salt embedded) — never the raw password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>owner | member. The first user in a workspace is the owner.</summary>
    public string Role { get; set; } = "owner";

    public bool EmailVerified { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
