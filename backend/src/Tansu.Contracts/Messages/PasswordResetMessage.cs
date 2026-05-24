namespace Tansu.Contracts.Messages;

public sealed record PasswordResetMessage(
    Guid UserId,
    string Email,
    string FullName,
    string TemporaryPassword,
    DateTimeOffset OccurredAt);
