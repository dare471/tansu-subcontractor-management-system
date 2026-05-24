namespace Tansu.Domain.Entities;

public class ApprovalSheetEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Guid ApproverUserId { get; set; }
    public int OrderNo { get; set; }

    /// <summary>Группа, объединяющая один цикл согласования сотрудника.</summary>
    public Guid RoundId { get; set; }

    public Guid? BatchId { get; set; }

    public string Status { get; set; } = Enums.ApprovalStatus.Pending;
    public DateTimeOffset? DecidedAt { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Employee? Employee { get; set; }
    public User? Approver { get; set; }
}
