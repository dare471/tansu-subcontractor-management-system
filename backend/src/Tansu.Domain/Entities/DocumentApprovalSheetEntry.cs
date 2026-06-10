namespace Tansu.Domain.Entities;

public class DocumentApprovalSheetEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentRequestId { get; set; }
    public Guid ApproverUserId { get; set; }
    public string ApproverRole { get; set; } = string.Empty;
    public int OrderNo { get; set; }
    public Guid RoundId { get; set; }
    public string Status { get; set; } = Enums.ApprovalStatus.Pending;
    public DateTimeOffset? DecidedAt { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? LastReminderAt { get; set; }
    public DateTimeOffset? EscalatedAt { get; set; }
    public Guid? ActingForUserId { get; set; }

    public DocumentRequest? DocumentRequest { get; set; }
    public User? Approver { get; set; }
}
