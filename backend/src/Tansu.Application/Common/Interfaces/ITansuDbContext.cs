using Microsoft.EntityFrameworkCore;
using Tansu.Domain.Entities;

namespace Tansu.Application.Common.Interfaces;

public interface ITansuDbContext
{
    DbSet<Subcontractor> Subcontractors { get; }
    DbSet<SubcontractorDocument> SubcontractorDocuments { get; }
    DbSet<ProjectRef> ProjectRefs { get; }
    DbSet<ProjectDocument> ProjectDocuments { get; }
    DbSet<ProjectSubcontractor> ProjectSubcontractors { get; }
    DbSet<User> Users { get; }
    DbSet<Employee> Employees { get; }
    DbSet<ApprovalMatrixEntry> ApprovalMatrix { get; }
    DbSet<ApprovalSheetEntry> ApprovalSheet { get; }
    DbSet<EmployeeApprovalBatch> EmployeeApprovalBatches { get; }
    DbSet<EmployeeApprovalBatchMember> EmployeeApprovalBatchMembers { get; }
    DbSet<DocumentRequest> DocumentRequests { get; }
    DbSet<DocumentRequestMatrixEntry> DocumentApprovalMatrix { get; }
    DbSet<DocumentApprovalSheetEntry> DocumentApprovalSheet { get; }
    DbSet<EmployeeAccessPass> EmployeeAccessPasses { get; }
    DbSet<EmployeeSiteVisit> EmployeeSiteVisits { get; }
    DbSet<EmployeeSafetyQuizCompletion> EmployeeSafetyQuizCompletions { get; }
    DbSet<EmployeePpeIssuance> EmployeePpeIssuances { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }
    DbSet<EmployeeBlockRecord> EmployeeBlockRecords { get; }
    DbSet<UserBlockRecord> UserBlockRecords { get; }
    DbSet<UserProjectAssignment> UserProjectAssignments { get; }
    DbSet<UserSubcontractorAssignment> UserSubcontractorAssignments { get; }
    DbSet<EmployeePhotoReview> EmployeePhotoReviews { get; }
    DbSet<AuditEvent> AuditEvents { get; }
    DbSet<ApproverDelegation> ApproverDelegations { get; }
    DbSet<ApprovalSlaPolicy> ApprovalSlaPolicies { get; }
    DbSet<SiteIncident> SiteIncidents { get; }
    DbSet<SiteIncidentEmployee> SiteIncidentEmployees { get; }
    DbSet<SiteIncidentComment> SiteIncidentComments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
