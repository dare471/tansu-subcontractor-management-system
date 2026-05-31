namespace Tansu.Domain.Entities;

public class UserProjectAssignment
{
    public Guid UserId { get; set; }
    public Guid ProjectOid { get; set; }

    public User? User { get; set; }
    public ProjectRef? Project { get; set; }
}
