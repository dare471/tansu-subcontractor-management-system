namespace Tansu.Domain.Entities;

public class ProjectSubcontractor
{
    public Guid ProjectOid { get; set; }
    public Guid SubcontractorId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public int CompletionPercent { get; set; }
    public DateTimeOffset? ProgressReportedAt { get; set; }
    public Guid? ProgressReportedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Subcontractor? Subcontractor { get; set; }
    public ProjectRef? Project { get; set; }
    public User? ProgressReportedBy { get; set; }
}
