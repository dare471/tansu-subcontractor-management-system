namespace Tansu.Domain.Entities;

/// <summary>
/// Факт прохода сотрудника на объект после успешной проверки Face ID.
/// </summary>
public class EmployeeSiteVisit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Guid? AccessPassId { get; set; }
    public DateTimeOffset CheckedInAt { get; set; } = DateTimeOffset.UtcNow;
    public double? FaceConfidence { get; set; }
    public string VerificationMethod { get; set; } = "face_id";

    public Employee? Employee { get; set; }
    public EmployeeAccessPass? AccessPass { get; set; }
}
