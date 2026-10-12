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
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__DefaultConnection to a MySQL connection string before running EF Core tooling.");
        }

        var options = new DbContextOptionsBuilder<DocGenieDbContext>()
            .UseMySQL(connectionString)
            .Options;

        return new DocGenieDbContext(options, new OrganizationContext());
    }
}
