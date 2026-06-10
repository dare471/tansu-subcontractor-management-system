namespace Tansu.Application.Auth;

public sealed record TansuPermissionsDto(
    bool CanRegisterSubcontractors,
    bool CanManageApprovalMatrix,
    bool CanApproveEmployees,
    bool CanBlockEmployees,
    bool CanViewVisitJournal,
    bool CanManageTansuUsers,
    bool CanManageSubordinates,
    bool CanViewEmployees,
    bool CanUploadDocuments,
    bool CanViewProjects,
    bool CanManageProjects,
    bool CanViewSubcontractors,
    bool CanReviewPhotos,
    bool IsReadOnlyMonitoring,
    bool IsGlobalAdmin,
    bool CanManageSubcontractorUsers,
    bool CanReassignSubcontractorManager,
    bool CanViewAuditLog,
    bool CanViewReports);

public sealed record TansuAccessContext(
    string? TansuRole,
    TansuPermissionsDto Permissions,
    IReadOnlySet<Guid>? VisibleSubcontractorIds,
    IReadOnlySet<Guid>? VisibleProjectOids,
    bool IncludeInactiveSubcontractors);
