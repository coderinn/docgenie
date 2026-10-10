using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LeadsPortal.Data;

/// <summary>
/// Allows EF tooling to create migrations without requiring the web host or a JWT secret.
/// </summary>
public sealed class DocGenieDbContextFactory : IDesignTimeDbContextFactory<DocGenieDbContext>
{
    public DocGenieDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=DocGenie;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<DocGenieDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new DocGenieDbContext(options, new OrganizationContext());
    }
}
