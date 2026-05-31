namespace Tansu.Domain.Entities;

/// <summary>
/// Документ сотрудника (удостоверение, сертификат, справка и т.д.).
/// </summary>
public class EmployeeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public Guid UploadedByUserId { get; set; }
    public Guid? SupersedesDocumentId { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset? ExpiryNotifiedAt { get; set; }

    public Employee? Employee { get; set; }
    public User? UploadedBy { get; set; }
    public EmployeeDocument? SupersedesDocument { get; set; }
}
