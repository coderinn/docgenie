namespace LeadsPortal.Models;

public enum TemplateFileType
{
    PDF = 1,
    DOCX = 2
}

public sealed class Template
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public TemplateFileType FileType { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Organization Organization { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<TemplateVersion> Versions { get; set; } = new List<TemplateVersion>();
}

public sealed class TemplateVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid TemplateId { get; set; }
    public int VersionNumber { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string SchemaMappingJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Template Template { get; set; } = null!;
    public ICollection<GenerationJob> GenerationJobs { get; set; } = new List<GenerationJob>();
}
