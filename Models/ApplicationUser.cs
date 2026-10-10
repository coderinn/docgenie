using Microsoft.AspNetCore.Identity;

namespace LeadsPortal.Models;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = new List<OrganizationMember>();
}
