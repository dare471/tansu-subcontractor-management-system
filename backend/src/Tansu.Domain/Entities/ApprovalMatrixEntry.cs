namespace Tansu.Domain.Entities;

public class ApprovalMatrixEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int OrderNo { get; set; }
    public Guid ProjectOid { get; set; }
    public Guid SubcontractorId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Subcontractor? Subcontractor { get; set; }
    public ProjectRef? Project { get; set; }
    public User? User { get; set; }
}
