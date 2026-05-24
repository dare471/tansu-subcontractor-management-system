namespace Tansu.Domain.Entities;

public class Subcontractor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Bin { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProjectSubcontractor> Projects { get; set; } = new List<ProjectSubcontractor>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
