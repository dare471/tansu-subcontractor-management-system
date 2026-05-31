using Tansu.Domain.Enums;

namespace Tansu.Domain.Entities;

public class EmployeeSiteVisit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Guid? AccessPassId { get; set; }
    public DateTimeOffset CheckedInAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CheckedOutAt { get; set; }
    public string? TerminalLocation { get; set; }
    public double? FaceConfidence { get; set; }
    public string VerificationMethod { get; set; } = "face_id";
    public string DataSource { get; set; } = SiteVisitDataSource.FaceId;

    public Employee? Employee { get; set; }
    public EmployeeAccessPass? AccessPass { get; set; }
}
