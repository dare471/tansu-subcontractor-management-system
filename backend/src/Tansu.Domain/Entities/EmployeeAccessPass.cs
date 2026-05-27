namespace Tansu.Domain.Entities;

/// <summary>
/// Пропуск с QR для идентификации сотрудника на объекте (после полного согласования).
/// </summary>
public class EmployeeAccessPass
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    /// <summary>Непредсказуемый токен, закодированный в QR.</summary>
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }

    public Employee? Employee { get; set; }
}
