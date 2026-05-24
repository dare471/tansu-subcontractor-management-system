namespace Tansu.Contracts.Messages;

public sealed record UserCreatedMessage(
    Guid UserId,
    string Email,
    string FullName,
    string UserType,
    Guid? SubcontractorId,
    string? TemporaryPassword,
    DateTimeOffset OccurredAt);
