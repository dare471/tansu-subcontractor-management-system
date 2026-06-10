namespace Tansu.IntegrationTests;

/// <summary>
/// Каталог HTTP-эндпоинтов API. При добавлении нового маршрута — добавьте запись сюда;
/// smoke-тест <see cref="ApiEndpointSmokeTests"/> и meta-тест <see cref="EndpointCoverageTests"/> упадут, если запись пропущена.
/// </summary>
public static class ApiEndpointCatalog
{
    public const int ExpectedCount = 118;

    public static IReadOnlyList<ApiEndpoint> All { get; } =
    [
        // Auth
        new("auth.login", HttpMethod.Post, "/api/auth/login", ApiAuthKind.Anonymous, ApiRequestBody.JsonMinimal),
        new("auth.dev-login", HttpMethod.Post, "/api/auth/dev-login", ApiAuthKind.Anonymous, ApiRequestBody.JsonMinimal),
        new("auth.change-password", HttpMethod.Post, "/api/auth/change-password", ApiAuthKind.Authenticated, ApiRequestBody.JsonEmpty),
        new("auth.me", HttpMethod.Get, "/api/auth/me", ApiAuthKind.Authenticated),
        new("auth.me-projects", HttpMethod.Get, "/api/auth/me/projects", ApiAuthKind.SubcontractorOnly),
        new("auth.me-project-progress", HttpMethod.Put, "/api/auth/me/projects/{projectOid}/progress", ApiAuthKind.SubcontractorOnly, ApiRequestBody.JsonMinimal),

        // Users
        new("users.list", HttpMethod.Get, "/api/users", ApiAuthKind.TansuOnly),
        new("users.create", HttpMethod.Post, "/api/users", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),
        new("users.update", HttpMethod.Put, "/api/users/{userId}", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),
        new("users.blocks", HttpMethod.Get, "/api/users/{userId}/blocks", ApiAuthKind.TansuOnly),
        new("users.reset-password", HttpMethod.Post, "/api/users/{userId}/reset-password", ApiAuthKind.TansuOnly),

        // Subcontractors
        new("subcontractors.list", HttpMethod.Get, "/api/subcontractors", ApiAuthKind.TansuOnly),
        new("subcontractors.create", HttpMethod.Post, "/api/subcontractors", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),
        new("subcontractors.update", HttpMethod.Put, "/api/subcontractors/{subcontractorId}", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),
        new("subcontractors.delete", HttpMethod.Delete, "/api/subcontractors/{missingId}", ApiAuthKind.TansuOnly),
        new("subcontractors.projects", HttpMethod.Get, "/api/subcontractors/{subcontractorId}/projects", ApiAuthKind.TansuOnly),
        new("subcontractors.bind-project", HttpMethod.Post, "/api/subcontractors/{subcontractorId}/projects", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),
        new("subcontractors.unbind-project", HttpMethod.Delete, "/api/subcontractors/{subcontractorId}/projects/{projectOid}", ApiAuthKind.TansuOnly),
        new("subcontractors.documents", HttpMethod.Get, "/api/subcontractors/{subcontractorId}/documents", ApiAuthKind.TansuOnly),
        new("subcontractors.upload-document", HttpMethod.Post, "/api/subcontractors/{subcontractorId}/documents", ApiAuthKind.TansuOnly),
        new("subcontractors.document-file", HttpMethod.Get, "/api/subcontractors/{subcontractorId}/documents/{subcontractorDocumentId}", ApiAuthKind.TansuOnly),
        new("subcontractors.delete-document", HttpMethod.Delete, "/api/subcontractors/{subcontractorId}/documents/{missingId}", ApiAuthKind.TansuOnly),

        // Projects
        new("projects.list", HttpMethod.Get, "/api/projects", ApiAuthKind.TansuOnly),
        new("projects.bind-options", HttpMethod.Get, "/api/projects/bind-options", ApiAuthKind.TansuOnly),
        new("projects.staff-options", HttpMethod.Get, "/api/projects/staff-options", ApiAuthKind.TansuOnly),
        new("projects.get", HttpMethod.Get, "/api/projects/{projectOid}", ApiAuthKind.TansuOnly),
        new("projects.update", HttpMethod.Put, "/api/projects/{projectOid}", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),
        new("projects.create", HttpMethod.Post, "/api/projects", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),
        new("projects.upload-document", HttpMethod.Post, "/api/projects/{projectOid}/documents", ApiAuthKind.TansuOnly),
        new("projects.document-file", HttpMethod.Get, "/api/projects/{projectOid}/documents/{projectDocumentId}", ApiAuthKind.TansuOnly),
        new("projects.delete-document", HttpMethod.Delete, "/api/projects/{projectOid}/documents/{missingId}", ApiAuthKind.TansuOnly),
        new("projects.bind-subcontractor", HttpMethod.Post, "/api/projects/{projectOid}/subcontractors", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),
        new("projects.update-subcontractor", HttpMethod.Put, "/api/projects/{projectOid}/subcontractors/{subcontractorId}", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),

        // Approval matrix
        new("matrix.list", HttpMethod.Get, "/api/approval-matrix", ApiAuthKind.TansuOnly),
        new("matrix.get", HttpMethod.Get, "/api/projects/{projectOid}/subcontractors/{subcontractorId}/matrix", ApiAuthKind.TansuOnly),
        new("matrix.set", HttpMethod.Put, "/api/projects/{projectOid}/subcontractors/{subcontractorId}/matrix", ApiAuthKind.TansuOnly, ApiRequestBody.JsonMinimal),

        // Employees
        new("employees.list", HttpMethod.Get, "/api/employees", ApiAuthKind.Authenticated),
        new("employees.create", HttpMethod.Post, "/api/employees", ApiAuthKind.Authenticated, ApiRequestBody.JsonEmpty),
        new("employees.update", HttpMethod.Put, "/api/employees/{employeeId}", ApiAuthKind.Authenticated, ApiRequestBody.JsonEmpty),
        new("employees.delete", HttpMethod.Delete, "/api/employees/{missingId}", ApiAuthKind.Authenticated),
        new("employees.upload-photo", HttpMethod.Post, "/api/employees/{employeeId}/photo", ApiAuthKind.Authenticated),
        new("employees.photo", HttpMethod.Get, "/api/employees/{employeeId}/photo", ApiAuthKind.Authenticated),
        new("employees.site-visits", HttpMethod.Get, "/api/employees/{employeeId}/site-visits", ApiAuthKind.Authenticated),
        new("employees.ppe", HttpMethod.Get, "/api/employees/{employeeId}/ppe", ApiAuthKind.Authenticated),
        new("employees.issue-ppe", HttpMethod.Post, "/api/employees/{employeeId}/ppe", ApiAuthKind.Authenticated, ApiRequestBody.JsonMinimal),
        new("employees.return-ppe", HttpMethod.Post, "/api/employees/{employeeId}/ppe/{issuanceId}/return", ApiAuthKind.Authenticated, ApiRequestBody.JsonEmpty),
        new("employees.documents", HttpMethod.Get, "/api/employees/{employeeId}/documents", ApiAuthKind.Authenticated),
        new("employees.upload-document", HttpMethod.Post, "/api/employees/{employeeId}/documents", ApiAuthKind.Authenticated),
        new("employees.document-file", HttpMethod.Get, "/api/employees/{employeeId}/documents/{documentId}", ApiAuthKind.Authenticated),
        new("employees.delete-document", HttpMethod.Delete, "/api/employees/{employeeId}/documents/{missingId}", ApiAuthKind.Authenticated),
        new("employees.blocks", HttpMethod.Get, "/api/employees/{employeeId}/blocks", ApiAuthKind.Authenticated),
        new("employees.block", HttpMethod.Post, "/api/employees/{employeeId}/block", ApiAuthKind.Authenticated, ApiRequestBody.JsonEmpty),

        // Photo reviews
        new("photo-reviews.pending", HttpMethod.Get, "/api/employees/photo-reviews/pending", ApiAuthKind.Authenticated),
        new("photo-reviews.status", HttpMethod.Get, "/api/employees/{employeeId}/photo-review", ApiAuthKind.Authenticated),
        new("photo-reviews.approve", HttpMethod.Post, "/api/employees/{employeeId}/photo-review/approve", ApiAuthKind.Authenticated, ApiRequestBody.JsonEmpty),
        new("photo-reviews.reject", HttpMethod.Post, "/api/employees/{employeeId}/photo-review/reject", ApiAuthKind.Authenticated, ApiRequestBody.JsonMinimal),

        // Access passes (staff)
        new("access-pass.get", HttpMethod.Get, "/api/employees/{employeeId}/access-pass", ApiAuthKind.Authenticated),
        new("access-pass.qr", HttpMethod.Get, "/api/employees/{employeeId}/access-pass/qr.png", ApiAuthKind.Authenticated),

        // Internal verify service
        new("internal.access-pass", HttpMethod.Get, "/api/internal/access-passes/{accessPassToken}", ApiAuthKind.VerifyServiceKey),
        new("internal.reference-photo", HttpMethod.Get, "/api/internal/access-passes/{accessPassToken}/reference-photo", ApiAuthKind.VerifyServiceKey),
        new("internal.check-in", HttpMethod.Post, "/api/internal/access-passes/{accessPassToken}/check-in", ApiAuthKind.VerifyServiceKey, ApiRequestBody.JsonEmpty),

        // Employee portal
        new("employee-portal.login", HttpMethod.Post, "/api/auth/employee/login", ApiAuthKind.Anonymous, ApiRequestBody.JsonMinimal),
        new("employee-portal.dashboard", HttpMethod.Get, "/api/employee-portal/dashboard", ApiAuthKind.EmployeeOnly),
        new("employee-portal.profile", HttpMethod.Get, "/api/employee-portal/profile", ApiAuthKind.EmployeeOnly),
        new("employee-portal.approvals", HttpMethod.Get, "/api/employee-portal/approvals", ApiAuthKind.EmployeeOnly),
        new("employee-portal.site-visits", HttpMethod.Get, "/api/employee-portal/site-visits", ApiAuthKind.EmployeeOnly),
        new("employee-portal.ppe", HttpMethod.Get, "/api/employee-portal/ppe", ApiAuthKind.EmployeeOnly),
        new("employee-portal.documents", HttpMethod.Get, "/api/employee-portal/documents", ApiAuthKind.EmployeeOnly),
        new("employee-portal.document-file", HttpMethod.Get, "/api/employee-portal/documents/{documentId}", ApiAuthKind.EmployeeOnly),
        new("employee-portal.blocks", HttpMethod.Get, "/api/employee-portal/blocks", ApiAuthKind.EmployeeOnly),
        new("employee-portal.upload-photo", HttpMethod.Post, "/api/employee-portal/photo", ApiAuthKind.EmployeeOnly),
        new("employee-portal.photo", HttpMethod.Get, "/api/employee-portal/photo", ApiAuthKind.EmployeeOnly),
        new("employee-portal.safety-quiz", HttpMethod.Get, "/api/employee-portal/safety-quiz", ApiAuthKind.EmployeeOnly, Query: "locale=en"),
        new("employee-portal.submit-quiz", HttpMethod.Post, "/api/employee-portal/safety-quiz", ApiAuthKind.EmployeeOnly, ApiRequestBody.JsonMinimal),
        new("employee-portal.qr", HttpMethod.Get, "/api/employee-portal/access-pass/qr.png", ApiAuthKind.EmployeeOnly),

        // Employee approvals
        new("approvals.submit", HttpMethod.Post, "/api/employees/{employeeId}/submit", ApiAuthKind.SubcontractorOnly),
        new("approvals.resubmit", HttpMethod.Post, "/api/employees/{employeeId}/resubmit", ApiAuthKind.SubcontractorOnly),
        new("approvals.history", HttpMethod.Get, "/api/employees/{employeeId}/approvals", ApiAuthKind.Authenticated),
        new("approvals.inbox", HttpMethod.Get, "/api/approvals/inbox", ApiAuthKind.Authenticated),
        new("approvals.approve", HttpMethod.Post, "/api/approvals/{sheetId}/approve", ApiAuthKind.Authenticated, ApiRequestBody.JsonEmpty),
        new("approvals.reject", HttpMethod.Post, "/api/approvals/{sheetId}/reject", ApiAuthKind.Authenticated, ApiRequestBody.JsonEmpty),

        // Employee batches
        new("batches.list", HttpMethod.Get, "/api/employee-batches", ApiAuthKind.SubcontractorOnly),
        new("batches.get", HttpMethod.Get, "/api/employee-batches/{batchId}", ApiAuthKind.SubcontractorOnly),
        new("batches.create", HttpMethod.Post, "/api/employee-batches", ApiAuthKind.SubcontractorOnly, ApiRequestBody.JsonEmpty),
        new("batches.add-members", HttpMethod.Post, "/api/employee-batches/{batchId}/members", ApiAuthKind.SubcontractorOnly, ApiRequestBody.JsonEmpty),
        new("batches.remove-member", HttpMethod.Delete, "/api/employee-batches/{batchId}/members/{employeeId}", ApiAuthKind.SubcontractorOnly),
        new("batches.submit", HttpMethod.Post, "/api/employee-batches/{batchId}/submit", ApiAuthKind.SubcontractorOnly),
        new("batches.delete", HttpMethod.Delete, "/api/employee-batches/{missingId}", ApiAuthKind.SubcontractorOnly),

        // Document requests
        new("document-requests.list", HttpMethod.Get, "/api/document-requests", ApiAuthKind.Authenticated),
        new("document-requests.create", HttpMethod.Post, "/api/document-requests", ApiAuthKind.SubcontractorOnly, ApiRequestBody.JsonEmpty),
        new("document-requests.update", HttpMethod.Put, "/api/document-requests/{documentRequestId}", ApiAuthKind.SubcontractorOnly, ApiRequestBody.JsonEmpty),
        new("document-requests.delete", HttpMethod.Delete, "/api/document-requests/{missingId}", ApiAuthKind.SubcontractorOnly),
        new("document-requests.submit", HttpMethod.Post, "/api/document-requests/{documentRequestId}/submit", ApiAuthKind.SubcontractorOnly),
        new("document-requests.resubmit", HttpMethod.Post, "/api/document-requests/{documentRequestId}/resubmit", ApiAuthKind.SubcontractorOnly),
        new("document-requests.approvals", HttpMethod.Get, "/api/document-requests/{documentRequestId}/approvals", ApiAuthKind.Authenticated),
        new("document-request-approvals.inbox", HttpMethod.Get, "/api/document-request-approvals/inbox", ApiAuthKind.TansuOnly),
        new("document-request-approvals.approve", HttpMethod.Post, "/api/document-request-approvals/{documentRequestSheetId}/approve", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),
        new("document-request-approvals.reject", HttpMethod.Post, "/api/document-request-approvals/{documentRequestSheetId}/reject", ApiAuthKind.TansuOnly, ApiRequestBody.JsonEmpty),
        new("document-matrix.summaries", HttpMethod.Get, "/api/document-matrix/summaries", ApiAuthKind.TansuOnly),
        new("document-matrix.get", HttpMethod.Get, "/api/document-matrix", ApiAuthKind.TansuOnly, Query: "projectOid={projectOid}&subcontractorId={subcontractorId}&requestType=leave"),
        new("document-matrix.set", HttpMethod.Put, "/api/document-matrix", ApiAuthKind.TansuOnly, ApiRequestBody.JsonMinimal, Query: "projectOid={projectOid}&subcontractorId={subcontractorId}&requestType=leave"),

        // Site visit journal
        new("site-visit-journal.list", HttpMethod.Get, "/api/site-visit-journal", ApiAuthKind.Authenticated),
        new("site-visit-journal.export", HttpMethod.Get, "/api/site-visit-journal/export", ApiAuthKind.Authenticated, Query: "format=csv"),

        // Audit & reports
        new("audit-events.list", HttpMethod.Get, "/api/audit-events", ApiAuthKind.TansuOnly),
        new("reports.approved-personnel-export", HttpMethod.Get, "/api/reports/approved-personnel/export", ApiAuthKind.TansuOnly, Query: "format=csv"),
        new("reports.site-visits-export", HttpMethod.Get, "/api/reports/site-visits/export", ApiAuthKind.TansuOnly, Query: "format=csv"),
        new("reports.employee-blocks-export", HttpMethod.Get, "/api/reports/employee-blocks/export", ApiAuthKind.TansuOnly, Query: "format=csv"),
        new("reports.document-requests-export", HttpMethod.Get, "/api/reports/document-requests/export", ApiAuthKind.Authenticated, Query: "format=csv"),
        new("reports.expiring-documents-export", HttpMethod.Get, "/api/reports/expiring-documents/export", ApiAuthKind.TansuOnly, Query: "format=csv"),
        new("reports.subcontractor-compliance", HttpMethod.Get, "/api/reports/subcontractor-compliance", ApiAuthKind.TansuOnly),

        // Delegations
        new("delegations.list", HttpMethod.Get, "/api/delegations", ApiAuthKind.Authenticated, Query: "activeOnly=true"),
        new("delegations.create", HttpMethod.Post, "/api/delegations", ApiAuthKind.TansuOnly, ApiRequestBody.JsonMinimal),
        new("delegations.revoke", HttpMethod.Delete, "/api/delegations/{missingId}", ApiAuthKind.TansuOnly),

        // Incidents
        new("incidents.list", HttpMethod.Get, "/api/incidents", ApiAuthKind.TansuOnly),
        new("incidents.create", HttpMethod.Post, "/api/incidents", ApiAuthKind.TansuOnly, ApiRequestBody.JsonMinimal),
        new("incidents.update", HttpMethod.Patch, "/api/incidents/{missingId}", ApiAuthKind.TansuOnly, ApiRequestBody.JsonMinimal),

        // ZUP
        new("zup.employees", HttpMethod.Get, "/api/zup/employees", ApiAuthKind.TansuOnly, Query: "company=tansu_construction"),
        new("zup.projects", HttpMethod.Get, "/api/zup/projects", ApiAuthKind.TansuOnly),
    ];
}

public enum ApiAuthKind
{
    Anonymous,
    Authenticated,
    TansuOnly,
    SubcontractorOnly,
    EmployeeOnly,
    VerifyServiceKey
}

public enum ApiRequestBody
{
    None,
    JsonEmpty,
    JsonMinimal,
    FormEmpty
}

public sealed record ApiEndpoint(
    string Id,
    HttpMethod Method,
    string PathTemplate,
    ApiAuthKind Auth,
    ApiRequestBody Body = ApiRequestBody.None,
    string? Query = null);
