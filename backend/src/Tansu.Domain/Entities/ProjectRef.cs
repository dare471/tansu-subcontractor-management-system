namespace Tansu.Domain.Entities;

public class ProjectRef
{
    public Guid ProjectOid { get; set; }
    public int? ZupId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ZupProjectManagerName { get; set; }
    public string? ContractType { get; set; }
    public DateTimeOffset? ZupSyncedAt { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string BudgetCurrency { get; set; } = "KZT";
    public Guid? ResponsibleAdminUserId { get; set; }
    public Guid? ProjectManagerUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? ResponsibleAdmin { get; set; }
    public User? ProjectManager { get; set; }
    public ICollection<ProjectDocument> Documents { get; set; } = new List<ProjectDocument>();
}
