namespace Tansu.Domain.Entities;

public class DocumentRequestMatrixEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectOid { get; set; }
    public Guid SubcontractorId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public int OrderNo { get; set; }

    public string ApproverRole { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ProjectRef? Project { get; set; }
    public Subcontractor? Subcontractor { get; set; }
}
