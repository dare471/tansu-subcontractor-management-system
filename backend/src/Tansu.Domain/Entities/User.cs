namespace Tansu.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public string UserType { get; set; } = Enums.UserType.Subcontractor;

    public Guid? SubcontractorId { get; set; }
    public Guid? EmployeeId { get; set; }

    public string? ApproverRole { get; set; }

    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Subcontractor? Subcontractor { get; set; }
    public Employee? Employee { get; set; }
}
