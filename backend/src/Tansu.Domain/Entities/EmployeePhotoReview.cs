namespace Tansu.Domain.Entities;

/// <summary>
/// История проверки эталонного фото сотрудника (авто / ручная).
/// </summary>
public class EmployeePhotoReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public string PhotoPath { get; set; } = string.Empty;
    public string ReviewType { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? DetailsJson { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Employee? Employee { get; set; }
    public User? ReviewedBy { get; set; }
}
