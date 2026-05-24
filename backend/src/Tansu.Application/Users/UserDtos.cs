namespace Tansu.Application.Users;

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Position,
    string Email,
    string UserType,
    Guid? SubcontractorId,
    string? SubcontractorName,
    string? ApproverRole,
    bool MustChangePassword,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateUserRequest(
    string FullName,
    string Position,
    string Email,
    string UserType,
    Guid? SubcontractorId,
    string? ApproverRole);

public sealed record UpdateUserRequest(
    string FullName,
    string Position,
    bool IsActive,
    string? ApproverRole);

public sealed record CreateUserResponse(UserDto User, string? TemporaryPassword);
