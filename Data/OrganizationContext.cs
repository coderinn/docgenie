namespace LeadsPortal.Data;

public interface IOrganizationContext
{
    Guid OrganizationId { get; }
    void SetOrganization(Guid organizationId);
}

public sealed class OrganizationContext : IOrganizationContext
{
    public Guid OrganizationId { get; private set; }

    public void SetOrganization(Guid organizationId)
    {
        OrganizationId = organizationId;
    }
}
