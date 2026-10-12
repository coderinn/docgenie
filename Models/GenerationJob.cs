namespace LeadsPortal.Models;

public enum GenerationJobStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    PartiallyFailed = 5
}

public enum JobRowStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3
}

public sealed class GenerationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid TemplateVersionId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public GenerationJobStatus Status { get; set; } = GenerationJobStatus.Pending;
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public TemplateVersion TemplateVersion { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<JobRow> Rows { get; set; } = new List<JobRow>();
}

public sealed class JobRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public int RowNumber { get; set; }
    public string InputDataJson { get; set; } = "{}";
    public JobRowStatus Status { get; set; } = JobRowStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string? OutputFilePath { get; set; }
    public string? VerificationCode { get; set; }

    public GenerationJob Job { get; set; } = null!;
}
