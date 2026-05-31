namespace Tansu.Domain.Entities;

public class ProjectDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectOid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid UploadedByUserId { get; set; }

    public ProjectRef? Project { get; set; }
    public User? UploadedBy { get; set; }
}
