namespace Tansu.Domain.Entities;

public class Subcontractor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Bin { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? RegisteredByUserId { get; set; }
    public Guid? ManagerUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? RegisteredBy { get; set; }
    public User? Manager { get; set; }

    public ICollection<ProjectSubcontractor> Projects { get; set; } = new List<ProjectSubcontractor>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
