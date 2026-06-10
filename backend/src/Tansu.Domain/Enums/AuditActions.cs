namespace Tansu.Domain.Enums;

public static class AuditActions
{
    public const string EmployeeSubmitted = "employee.submitted";
    public const string EmployeeApproved = "employee.approved";
    public const string EmployeeRejected = "employee.rejected";
    public const string EmployeeBatchSubmitted = "employee_batch.submitted";
    public const string EmployeeBlocked = "employee.blocked";
    public const string EmployeeUnblocked = "employee.unblocked";
    public const string MatrixUpdated = "matrix.updated";
    public const string DocumentMatrixUpdated = "document_matrix.updated";
    public const string DocumentRequestSubmitted = "document_request.submitted";
    public const string DocumentRequestApproved = "document_request.approved";
    public const string DocumentRequestRejected = "document_request.rejected";
    public const string UserCreated = "user.created";
    public const string UserBlocked = "user.blocked";
    public const string UserUnblocked = "user.unblocked";
    public const string AccessPassIssued = "access_pass.issued";
    public const string PortalProvisioned = "portal.provisioned";
    public const string PortalDeactivated = "portal.deactivated";
    public const string PhotoApproved = "photo.approved";
    public const string PhotoRejected = "photo.rejected";
    public const string DelegationCreated = "delegation.created";
    public const string DelegationRevoked = "delegation.revoked";
    public const string IncidentCreated = "incident.created";
    public const string IncidentUpdated = "incident.updated";
    public const string IncidentResolved = "incident.resolved";
}
