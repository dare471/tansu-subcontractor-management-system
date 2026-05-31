namespace Tansu.Application.Employees;

public sealed record EmployeeDto(
    Guid Id,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    string? ProjectName,
    string FullName,
    string Position,
    string Phone,
    string Iin,
    string? PhotoPath,
    string? PhotoReviewStatus,
    string? PhotoReviewReason,
    bool IsBlocked,
    string? BlockReason,
    string? CurrentStatus,
    Guid? DraftBatchId,
    string? DraftBatchTitle,
    Guid? SubmittedBatchId,
    string? SubmittedBatchTitle,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateEmployeeRequest(
    Guid ProjectOid,
    string FullName,
    string Position,
    string Phone,
    string Iin);

public sealed record UpdateEmployeeRequest(
    string FullName,
    string Position,
    string Phone,
    string Iin);
