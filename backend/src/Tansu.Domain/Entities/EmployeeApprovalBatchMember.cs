namespace Tansu.Domain.Entities;

public class EmployeeApprovalBatchMember
{
    public Guid BatchId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;

    public EmployeeApprovalBatch? Batch { get; set; }
    public Employee? Employee { get; set; }
}
