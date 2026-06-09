namespace Tansu.Domain.Entities;

public class ApproverDelegation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DelegatorUserId { get; set; }
    public Guid DelegateUserId { get; set; }
    public Guid? ProjectOid { get; set; }
    public Guid? SubcontractorId { get; set; }
    public string? ApproverRole { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? Delegator { get; set; }
    public User? Delegate { get; set; }
}
