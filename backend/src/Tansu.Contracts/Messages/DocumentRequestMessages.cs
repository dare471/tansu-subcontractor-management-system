namespace Tansu.Contracts.Messages;

public sealed record DocumentRequestSubmittedMessage(
    Guid RequestId,
    string RequestType,
    string Title,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    string? ProjectName,
    Guid InitiatorUserId,
    string InitiatorEmail,
    Guid FirstApproverUserId,
    string FirstApproverEmail,
    string FirstApproverFullName,
    string FirstApproverRole,
    DateTimeOffset OccurredAt);

public sealed record DocumentRequestNextApproverMessage(
    Guid RequestId,
    string RequestType,
    string Title,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    string? ProjectName,
    Guid ApproverUserId,
    string ApproverEmail,
    string ApproverFullName,
    string ApproverRole,
    int OrderNo,
    DateTimeOffset OccurredAt);

public sealed record DocumentRequestDecisionMessage(
    Guid RequestId,
    string RequestType,
    string Title,
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

public sealed record DocumentRequestFullyApprovedMessage(
    Guid RequestId,
    string RequestType,
    string Title,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    Guid InitiatorUserId,
    string InitiatorEmail,
    DateTimeOffset OccurredAt);
