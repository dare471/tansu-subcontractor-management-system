namespace Tansu.Domain.Entities;

public class ProjectRef
{
    public Guid ProjectOid { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
