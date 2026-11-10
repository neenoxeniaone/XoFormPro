namespace FormSubmissionPro.Api.Domain;

/// <summary>An invitation for someone to join a workspace as a team member.</summary>
public class Invitation
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "member";

    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
