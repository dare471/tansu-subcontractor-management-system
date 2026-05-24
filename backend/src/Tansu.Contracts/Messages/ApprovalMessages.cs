namespace Tansu.Contracts.Messages;

public sealed record ApprovalSubmittedMessage(
    Guid EmployeeId,
    string EmployeeFullName,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    Guid InitiatorUserId,
    string InitiatorEmail,
    Guid FirstApproverUserId,
    string FirstApproverEmail,
    string FirstApproverFullName,
    DateTimeOffset OccurredAt);

public sealed record EmployeeApprovalDecisionMessage(
    Guid EmployeeId,
    string EmployeeFullName,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    Guid ApproverUserId,
    string ApproverEmail,
    string ApproverFullName,
    string Decision,
    string? Comment,
    Guid InitiatorUserId,
    string InitiatorEmail,
    DateTimeOffset OccurredAt);

public sealed record NextApproverNotificationMessage(
    Guid EmployeeId,
    string EmployeeFullName,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    Guid ApproverUserId,
    string ApproverEmail,
    string ApproverFullName,
    int OrderNo,
    DateTimeOffset OccurredAt);

public sealed record EmployeeFullyApprovedMessage(
    Guid EmployeeId,
    string EmployeeFullName,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    Guid InitiatorUserId,
    string InitiatorEmail,
    DateTimeOffset OccurredAt);

public sealed record EmployeeBatchMemberInfo(
    Guid EmployeeId,
    string FullName,
    string Position);

public sealed record EmployeeBatchSubmittedMessage(
    Guid BatchId,
    string BatchTitle,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    string? ProjectName,
    Guid InitiatorUserId,
    string InitiatorEmail,
    Guid FirstApproverUserId,
    string FirstApproverEmail,
    string FirstApproverFullName,
    IReadOnlyList<EmployeeBatchMemberInfo> Employees,
    DateTimeOffset OccurredAt);
