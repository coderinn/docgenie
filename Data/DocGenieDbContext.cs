using LeadsPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LeadsPortal.Data;

public sealed class DocGenieDbContext(
    DbContextOptions<DocGenieDbContext> options,
    IOrganizationContext organizationContext)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    private static readonly ValueConverter<DateTimeOffset, DateTime> UtcDateTimeOffsetConverter = new(
        value => value.UtcDateTime,
        value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

    private readonly IOrganizationContext _organizationContext = organizationContext;

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<OrganizationInvitation> OrganizationInvitations => Set<OrganizationInvitation>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();
    public DbSet<GenerationJob> GenerationJobs => Set<GenerationJob>();
    public DbSet<JobRow> JobRows => Set<JobRow>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(100).IsRequired();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasKey(organization => organization.Id);
            entity.Property(organization => organization.Name).HasMaxLength(200).IsRequired();
            entity.Property(organization => organization.Slug).HasMaxLength(120).IsRequired();
            entity.HasIndex(organization => organization.Slug).IsUnique();
            entity.HasQueryFilter(organization =>
                _organizationContext.OrganizationId != Guid.Empty
                && organization.Id == _organizationContext.OrganizationId);
        });

        modelBuilder.Entity<OrganizationMember>(entity =>
        {
            entity.ToTable("OrganizationMembers", table =>
                table.HasCheckConstraint("CK_OrganizationMembers_Role", "`Role` IN ('Owner', 'Admin', 'Member', 'Viewer')"));
            entity.HasKey(member => new { member.OrganizationId, member.UserId });
            entity.Property(member => member.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(member => member.UserId);
            entity.HasQueryFilter(member =>
                _organizationContext.OrganizationId != Guid.Empty
                && member.OrganizationId == _organizationContext.OrganizationId);
            entity.HasOne(member => member.Organization)
                .WithMany(organization => organization.Members)
                .HasForeignKey(member => member.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(member => member.User)
                .WithMany(user => user.OrganizationMemberships)
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationInvitation>(entity =>
        {
            entity.ToTable("OrganizationInvitations", table =>
                table.HasCheckConstraint("CK_OrganizationInvitations_Role", "`Role` IN ('Owner', 'Admin', 'Member', 'Viewer')"));
            entity.HasKey(invitation => invitation.Id);
            entity.Property(invitation => invitation.Email).HasMaxLength(256).IsRequired();
            entity.Property(invitation => invitation.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(invitation => invitation.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(invitation => new { invitation.OrganizationId, invitation.Email });
            entity.HasQueryFilter(invitation =>
                _organizationContext.OrganizationId != Guid.Empty
                && invitation.OrganizationId == _organizationContext.OrganizationId);
            entity.HasOne(invitation => invitation.Organization)
                .WithMany(organization => organization.Invitations)
                .HasForeignKey(invitation => invitation.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(invitation => invitation.InvitedByUser)
                .WithMany()
                .HasForeignKey(invitation => invitation.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Template>(entity =>
        {
            entity.ToTable("Templates", table =>
                table.HasCheckConstraint("CK_Templates_FileType", "`FileType` IN ('PDF', 'DOCX')"));
            entity.HasKey(template => template.Id);
            entity.Property(template => template.Name).HasMaxLength(200).IsRequired();
            entity.Property(template => template.Description).HasMaxLength(2000);
            entity.Property(template => template.StoragePath).HasMaxLength(1024).IsRequired();
            entity.Property(template => template.FileType).HasConversion<string>().HasMaxLength(10).IsRequired();
            entity.HasIndex(template => new { template.OrganizationId, template.Name }).IsUnique();
            entity.HasAlternateKey(template => new { template.Id, template.OrganizationId });
            entity.HasQueryFilter(template =>
                _organizationContext.OrganizationId != Guid.Empty
                && template.OrganizationId == _organizationContext.OrganizationId);
            entity.HasOne(template => template.Organization)
                .WithMany(organization => organization.Templates)
                .HasForeignKey(template => template.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(template => template.CreatedByUser)
                .WithMany()
                .HasForeignKey(template => template.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TemplateVersion>(entity =>
        {
            entity.ToTable("TemplateVersions");
            entity.HasKey(version => version.Id);
            entity.Property(version => version.FilePath).HasMaxLength(1024).IsRequired();
            entity.Property(version => version.SchemaMappingJson).HasColumnType("longtext").IsRequired();
            entity.HasIndex(version => new { version.TemplateId, version.VersionNumber }).IsUnique();
            entity.HasAlternateKey(version => new { version.Id, version.OrganizationId });
            entity.HasQueryFilter(version =>
                _organizationContext.OrganizationId != Guid.Empty
                && version.OrganizationId == _organizationContext.OrganizationId);
            entity.HasOne(version => version.Template)
                .WithMany(template => template.Versions)
                .HasForeignKey(version => new { version.TemplateId, version.OrganizationId })
                .HasPrincipalKey(template => new { template.Id, template.OrganizationId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GenerationJob>(entity =>
        {
            entity.ToTable("GenerationJobs", table =>
            {
                table.HasCheckConstraint("CK_GenerationJobs_Status", "`Status` IN ('Pending', 'Processing', 'Completed', 'Failed', 'PartiallyFailed')");
                table.HasCheckConstraint("CK_GenerationJobs_RowCounts", "`TotalRows` >= 0 AND `SuccessCount` >= 0 AND `FailedCount` >= 0 AND `SuccessCount` + `FailedCount` <= `TotalRows`");
            });
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.HasIndex(job => new { job.OrganizationId, job.CreatedAt });
            entity.HasQueryFilter(job =>
                _organizationContext.OrganizationId != Guid.Empty
                && job.OrganizationId == _organizationContext.OrganizationId);
            entity.HasOne(job => job.Organization)
                .WithMany(organization => organization.GenerationJobs)
                .HasForeignKey(job => job.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(job => job.TemplateVersion)
                .WithMany(version => version.GenerationJobs)
                .HasForeignKey(job => new { job.TemplateVersionId, job.OrganizationId })
                .HasPrincipalKey(version => new { version.Id, version.OrganizationId })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(job => job.CreatedByUser)
                .WithMany()
                .HasForeignKey(job => job.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobRow>(entity =>
        {
            entity.ToTable("JobRows", table =>
            {
                table.HasCheckConstraint("CK_JobRows_Status", "`Status` IN ('Pending', 'Success', 'Failed')");
                table.HasCheckConstraint("CK_JobRows_RowNumber", "`RowNumber` > 0");
            });
            entity.HasKey(row => row.Id);
            entity.Property(row => row.InputDataJson).HasColumnType("longtext").IsRequired();
            entity.Property(row => row.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(row => row.ErrorMessage).HasMaxLength(4000);
            entity.Property(row => row.OutputFilePath).HasMaxLength(1024);
            entity.Property(row => row.VerificationCode).HasMaxLength(100);
            entity.HasIndex(row => new { row.JobId, row.RowNumber }).IsUnique();
            entity.HasIndex(row => row.VerificationCode).IsUnique();
            entity.HasQueryFilter(row =>
                _organizationContext.OrganizationId != Guid.Empty
                && row.Job.OrganizationId == _organizationContext.OrganizationId);
            entity.HasOne(row => row.Job)
                .WithMany(job => job.Rows)
                .HasForeignKey(row => row.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Action).HasMaxLength(100).IsRequired();
            entity.Property(log => log.EntityName).HasMaxLength(200).IsRequired();
            entity.Property(log => log.EntityId).HasMaxLength(200).IsRequired();
            entity.Property(log => log.DetailsJson).HasColumnType("longtext");
            entity.HasIndex(log => new { log.OrganizationId, log.Timestamp });
            entity.HasQueryFilter(log =>
                _organizationContext.OrganizationId != Guid.Empty
                && log.OrganizationId == _organizationContext.OrganizationId);
            entity.HasOne(log => log.Organization)
                .WithMany(organization => organization.AuditLogs)
                .HasForeignKey(log => log.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(log => log.User)
                .WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MySQL has no datetimeoffset column type. Store the instant in UTC and
        // restore the UTC offset when EF materializes DateTimeOffset properties.
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entityType => entityType.GetProperties())
                     .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTimeOffset)))
        {
            property.SetValueConverter(UtcDateTimeOffsetConverter);
            property.SetColumnType("datetime(6)");
        }
    }
}
