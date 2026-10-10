namespace LeadsPortal.Models;

public sealed class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public ICollection<Template> Templates { get; set; } = new List<Template>();
    public ICollection<GenerationJob> GenerationJobs { get; set; } = new List<GenerationJob>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<OrganizationInvitation> Invitations { get; set; } = new List<OrganizationInvitation>();
}
