namespace Tansu.Domain.Entities;

public class UserBlockRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
    public User? InitiatedBy { get; set; }
}
