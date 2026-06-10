namespace Tansu.Domain.Entities;

public class ApprovalSlaPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Scope { get; set; } = "global";
    public Guid? ProjectOid { get; set; }
    public string? RequestType { get; set; }
    public int PendingDaysWarning { get; set; } = 2;
    public int PendingDaysEscalation { get; set; } = 3;
    public string? EscalationRole { get; set; }
    public Guid? EscalationUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
