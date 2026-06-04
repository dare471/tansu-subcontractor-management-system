namespace Tansu.Domain.Entities;

public class EmployeeAccessPass
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }

    public Employee? Employee { get; set; }
}
