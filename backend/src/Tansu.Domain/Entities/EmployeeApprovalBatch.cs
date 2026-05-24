namespace Tansu.Domain.Entities;

public class EmployeeApprovalBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubcontractorId { get; set; }
    public Guid ProjectOid { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = Enums.BatchStatus.Draft;
    public int EmployeeCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }

    public Subcontractor? Subcontractor { get; set; }
    public ProjectRef? Project { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<EmployeeApprovalBatchMember> Members { get; set; } = new List<EmployeeApprovalBatchMember>();
}
