namespace Tansu.Contracts.Messages;

public sealed record ApprovalSlaWarningMessage(
    string WorkflowType,
    Guid SheetId,
    Guid ApproverUserId,
    string ApproverEmail,
    string ApproverFullName,
    string SubjectTitle,
    int PendingDays,
    DateTimeOffset OccurredAt);

public sealed record ApprovalEscalationMessage(
    string WorkflowType,
    Guid SheetId,
    string SubjectTitle,
    string EscalationEmail,
    string? EscalationFullName,
    int PendingDays,
    DateTimeOffset OccurredAt);
