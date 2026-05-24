namespace Tansu.Domain.Entities;

public class ProjectSubcontractor
{
    public Guid ProjectOid { get; set; }
    public Guid SubcontractorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Subcontractor? Subcontractor { get; set; }
    public ProjectRef? Project { get; set; }
}
