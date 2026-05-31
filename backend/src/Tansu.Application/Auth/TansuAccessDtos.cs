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
    bool IsReadOnlyMonitoring,
    bool IsGlobalAdmin);

public sealed record TansuAccessContext(
    string? TansuRole,
    TansuPermissionsDto Permissions,
    IReadOnlySet<Guid>? VisibleSubcontractorIds,
    IReadOnlySet<Guid>? VisibleProjectOids,
    bool IncludeInactiveSubcontractors);
