namespace Tansu.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubcontractorId { get; set; }
    public Guid ProjectOid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Iin { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public string? PhotoReviewStatus { get; set; }
    public string? PhotoReviewReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Subcontractor? Subcontractor { get; set; }
    public ProjectRef? Project { get; set; }
    public ICollection<ApprovalSheetEntry> ApprovalSheet { get; set; } = new List<ApprovalSheetEntry>();
}
