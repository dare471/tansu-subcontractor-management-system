namespace Tansu.Domain.Entities;

public class UserSubcontractorAssignment
{
    public Guid UserId { get; set; }
    public Guid SubcontractorId { get; set; }

    public User? User { get; set; }
    public Subcontractor? Subcontractor { get; set; }
}
