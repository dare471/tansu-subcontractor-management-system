using Tansu.Domain.Enums;

namespace Tansu.Domain.Entities;

/// <summary>
/// Выдача СИЗ сотруднику (каска, униформа и т.д.).
/// </summary>
public class EmployeePpeIssuance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public string ItemType { get; set; } = PpeItemType.Helmet;
    public string? Size { get; set; }
    public string? InventoryNumber { get; set; }
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid IssuedByUserId { get; set; }
    public DateTimeOffset? ReturnedAt { get; set; }
    public string? Notes { get; set; }

    public Employee? Employee { get; set; }
    public User? IssuedBy { get; set; }
}
