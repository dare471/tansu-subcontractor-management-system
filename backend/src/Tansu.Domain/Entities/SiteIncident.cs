namespace Tansu.Domain.Entities;

public class SiteIncident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectOid { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid ReportedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "medium";
    public string Status { get; set; } = "open";
    public Guid? SubcontractorId { get; set; }
    public bool BlockUntilResolved { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ProjectRef? Project { get; set; }
    public User? ReportedBy { get; set; }
    public Subcontractor? Subcontractor { get; set; }
    public ICollection<SiteIncidentEmployee> LinkedEmployees { get; set; } = new List<SiteIncidentEmployee>();
    public ICollection<SiteIncidentComment> Comments { get; set; } = new List<SiteIncidentComment>();
}

public class SiteIncidentEmployee
{
    public Guid IncidentId { get; set; }
    public Guid EmployeeId { get; set; }
    public SiteIncident? Incident { get; set; }
    public Employee? Employee { get; set; }
}

public class SiteIncidentComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncidentId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public SiteIncident? Incident { get; set; }
    public User? Author { get; set; }
}
