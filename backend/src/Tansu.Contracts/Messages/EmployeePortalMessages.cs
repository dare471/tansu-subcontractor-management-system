namespace Tansu.Contracts.Messages;

public sealed record EmployeeQuizReminderMessage(
    Guid EmployeeId,
    string EmployeeFullName,
    string Email,
    Guid SubcontractorId,
    string SubcontractorName,
    DateTimeOffset OccurredAt);
