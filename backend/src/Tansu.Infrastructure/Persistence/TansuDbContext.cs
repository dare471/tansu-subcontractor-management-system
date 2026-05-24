using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence;

public class TansuDbContext : DbContext, ITansuDbContext
{
    public const string Schema = "subcontract";

    public TansuDbContext(DbContextOptions<TansuDbContext> options) : base(options) { }

    public DbSet<Subcontractor> Subcontractors => Set<Subcontractor>();
    public DbSet<ProjectRef> ProjectRefs => Set<ProjectRef>();
    public DbSet<ProjectSubcontractor> ProjectSubcontractors => Set<ProjectSubcontractor>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ApprovalMatrixEntry> ApprovalMatrix => Set<ApprovalMatrixEntry>();
    public DbSet<ApprovalSheetEntry> ApprovalSheet => Set<ApprovalSheetEntry>();
    public DbSet<EmployeeApprovalBatch> EmployeeApprovalBatches => Set<EmployeeApprovalBatch>();
    public DbSet<EmployeeApprovalBatchMember> EmployeeApprovalBatchMembers => Set<EmployeeApprovalBatchMember>();
    public DbSet<DocumentRequest> DocumentRequests => Set<DocumentRequest>();
    public DbSet<DocumentRequestMatrixEntry> DocumentApprovalMatrix => Set<DocumentRequestMatrixEntry>();
    public DbSet<DocumentApprovalSheetEntry> DocumentApprovalSheet => Set<DocumentApprovalSheetEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Schema);
        b.ApplyConfigurationsFromAssembly(typeof(TansuDbContext).Assembly);
    }
}
