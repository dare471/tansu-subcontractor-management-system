using Tansu.Domain.Enums;

namespace Tansu.Domain.Entities;

/// <summary>
/// Запись о блокировке или разблокировке сотрудника.
/// </summary>
public class EmployeeBlockRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = EmployeeBlockRequestStatus.Applied;
    public string? InitiatorRole { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Employee? Employee { get; set; }
    public User? InitiatedBy { get; set; }
}
