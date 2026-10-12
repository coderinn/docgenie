namespace LeadsPortal.Models;

/// <summary>
/// Pending organization invitation. Only a SHA-256 hash of the single-use token is persisted.
/// </summary>
public sealed class OrganizationInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public OrganizationRole Role { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ApplicationUser InvitedByUser { get; set; } = null!;
}
