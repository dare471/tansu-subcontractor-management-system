namespace Tansu.Application.Approvals;

public sealed record InboxItemDto(
    Guid SheetId,
    Guid EmployeeId,
    string EmployeeFullName,
    string Position,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    string? ProjectName,
    int OrderNo,
    DateTimeOffset SubmittedAt,
    Guid? BatchId,
    string? BatchTitle);

public sealed record ApprovalBatchDto(
    Guid Id,
    string Title,
    string Status,
    Guid ProjectOid,
    string? ProjectName,
    int EmployeeCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    IReadOnlyList<ApprovalBatchEmployeeDto> Employees);

public sealed record ApprovalBatchEmployeeDto(
    Guid EmployeeId,
    string FullName,
    string Position,
    string? CurrentStatus);

public sealed record CreateEmployeeBatchRequest(Guid ProjectOid, string Title);

public sealed record AddEmployeesToBatchRequest(IReadOnlyList<Guid> EmployeeIds);

public sealed record BatchSubmitResultDto(
    Guid BatchId,
    string Title,
    int SubmittedCount,
    IReadOnlyList<BatchSubmitItemDto> Items);

public sealed record BatchSubmitItemDto(Guid EmployeeId, Guid RoundId);

public sealed record ApprovalHistoryRowDto(
    Guid SheetId,
    Guid RoundId,
    int OrderNo,
    Guid ApproverUserId,
    string ApproverFullName,
    string Status,
    string? Comment,
    DateTimeOffset? DecidedAt,
    DateTimeOffset CreatedAt);

public sealed record ApprovalRoundSummaryDto(
    Guid RoundId,
    string OverallStatus,
    IReadOnlyList<ApprovalHistoryRowDto> Steps);

public sealed record EmployeeApprovalsDto(
    Guid EmployeeId,
    string CurrentStatus,
    IReadOnlyList<ApprovalRoundSummaryDto> Rounds);

public sealed record DecisionRequest(string? Comment);
