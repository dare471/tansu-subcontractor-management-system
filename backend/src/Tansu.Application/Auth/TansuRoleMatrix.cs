using Tansu.Domain.Enums;

namespace Tansu.Application.Auth;

public static class TansuRoleMatrix
{
    public static TansuPermissionsDto Resolve(string? role, bool isSuperUser) =>
        isSuperUser || role == TansuRole.GlobalAdmin ? GlobalAdmin() : role switch
        {
            TansuRole.OidManager => new(
                CanRegisterSubcontractors: true,
                CanManageApprovalMatrix: true,
                CanApproveEmployees: true,
                CanBlockEmployees: false,
                CanViewVisitJournal: false,
                CanManageTansuUsers: false,
                CanManageSubordinates: false,
                CanViewEmployees: true,
                CanUploadDocuments: true,
                CanViewProjects: true,
                CanManageProjects: true,
                CanViewSubcontractors: true,
                CanReviewPhotos: false,
                IsReadOnlyMonitoring: false,
                IsGlobalAdmin: false,
                CanManageSubcontractorUsers: true,
                CanReassignSubcontractorManager: false,
                CanViewAuditLog: false,
                CanViewReports: true),
            TansuRole.OidDirector => new(
                CanRegisterSubcontractors: false,
                CanManageApprovalMatrix: false,
                CanApproveEmployees: true,
                CanBlockEmployees: true,
                CanViewVisitJournal: false,
                CanManageTansuUsers: false,
                CanManageSubordinates: true,
                CanViewEmployees: true,
                CanUploadDocuments: true,
                CanViewProjects: true,
                CanManageProjects: false,
                CanViewSubcontractors: true,
                CanReviewPhotos: false,
                IsReadOnlyMonitoring: false,
                IsGlobalAdmin: false,
                CanManageSubcontractorUsers: false,
                CanReassignSubcontractorManager: true,
                CanViewAuditLog: true,
                CanViewReports: true),
            TansuRole.SbProject => new(
                false, false, false, true, false, false, false, true, false,
                true, false, true, true, false, false, false, false, false, true),
            TansuRole.SbChief => new(
                false, false, false, true, true, false, false, true, false,
                true, false, true, true, false, false, false, false, true, true),
            TansuRole.SafetyProject => new(
                false, false, false, true, false, false, false, true, false,
                true, false, true, true, false, false, false, false, false, true),
            TansuRole.SafetyChief => new(
                false, false, false, true, true, false, false, true, false,
                true, false, true, true, false, false, false, false, true, true),
            TansuRole.ProjectManager => new(
                false, false, false, false, true, false, false, true, false,
                true, false, true, false, true, false, false, false, false, true),
            _ => DenyAll()
        };

    public static TansuPermissionsDto GlobalAdmin() => new(
        CanRegisterSubcontractors: true,
        CanManageApprovalMatrix: true,
        CanApproveEmployees: true,
        CanBlockEmployees: true,
        CanViewVisitJournal: true,
        CanManageTansuUsers: true,
        CanManageSubordinates: true,
        CanViewEmployees: true,
        CanUploadDocuments: true,
        CanViewProjects: true,
        CanManageProjects: true,
        CanViewSubcontractors: true,
        CanReviewPhotos: true,
        IsReadOnlyMonitoring: false,
        IsGlobalAdmin: true,
        CanManageSubcontractorUsers: true,
        CanReassignSubcontractorManager: true,
        CanViewAuditLog: true,
        CanViewReports: true);

    public static TansuPermissionsDto SubcontractorPortal() => new(
        false, false, false, false, false, false, false, true, true,
        false, false, false, false, false, false, false, false, false, true);

    public static TansuPermissionsDto DenyAll() => new(
        false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false);

    /// <summary>Разделы меню по роли.</summary>
    public static IReadOnlyList<string> MenuSectionsForRole(string? role) => role switch
    {
        TansuRole.GlobalAdmin =>
            ["home", "subcontractors", "projects", "users", "tansu-employees", "matrix",
                "document-matrix", "site-visit-journal", "audit-log", "reports", "incidents",
                "approvals-inbox", "photo-reviews-inbox", "document-requests-inbox"],
        TansuRole.OidManager =>
            ["home", "subcontractors", "projects", "users", "tansu-employees", "matrix",
                "approvals-inbox", "document-requests-inbox"],
        TansuRole.OidDirector =>
            ["home", "subcontractors", "projects", "tansu-employees", "approvals-inbox",
                "document-requests-inbox"],
        TansuRole.SbProject or TansuRole.SafetyProject =>
            ["home", "subcontractors", "projects", "tansu-employees", "photo-reviews-inbox"],
        TansuRole.SbChief or TansuRole.SafetyChief =>
            ["home", "subcontractors", "projects", "tansu-employees", "site-visit-journal",
                "photo-reviews-inbox"],
        TansuRole.ProjectManager =>
            ["home", "subcontractors", "projects", "tansu-employees", "site-visit-journal"],
        _ => ["home"]
    };
}
