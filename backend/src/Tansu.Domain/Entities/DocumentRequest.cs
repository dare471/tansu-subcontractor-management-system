namespace Tansu.Domain.Entities;

public class DocumentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubcontractorId { get; set; }
    public Guid ProjectOid { get; set; }
    public Guid CreatedByUserId { get; set; }

    public string RequestType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Subcontractor? Subcontractor { get; set; }
    public ProjectRef? Project { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<DocumentApprovalSheetEntry> ApprovalSheet { get; set; } =
        new List<DocumentApprovalSheetEntry>();
}
