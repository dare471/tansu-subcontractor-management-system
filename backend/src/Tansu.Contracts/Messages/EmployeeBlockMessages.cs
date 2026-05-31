namespace Tansu.Contracts.Messages;

public sealed record EmployeeBlockedMessage(
    Guid EmployeeId,
    string EmployeeFullName,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    string? ProjectName,
    Guid InitiatorUserId,
    string InitiatorFullName,
    string InitiatorRole,
    string Reason,
    IReadOnlyList<string> NotifyEmails,
    DateTimeOffset OccurredAt);

public sealed record EmployeeDocumentExpiringMessage(
    Guid DocumentId,
    Guid EmployeeId,
    string EmployeeFullName,
    string DocumentName,
    string DocumentType,
    DateTimeOffset ExpiresAt,
    Guid SubcontractorId,
    string SubcontractorName,
    IReadOnlyList<string> NotifyEmails);
