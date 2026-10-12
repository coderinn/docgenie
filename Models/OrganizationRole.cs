namespace LeadsPortal.Models;

public enum OrganizationRole
{
    Viewer = 1,
    Member = 2,
    Admin = 3,
    Owner = 4
}

public sealed class OrganizationMember
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public OrganizationRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
