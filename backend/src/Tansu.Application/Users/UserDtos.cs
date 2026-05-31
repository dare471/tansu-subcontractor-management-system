namespace Tansu.Application.Users;

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Position,
    string Email,
    string UserType,
    Guid? SubcontractorId,
    string? SubcontractorName,
    Guid? EmployeeId,
    string? ApproverRole,
    string? TansuRole,
    Guid? ManagerUserId,
    IReadOnlyList<Guid> ProjectOids,
    IReadOnlyList<string> ProjectNames,
    IReadOnlyList<Guid> SubcontractorIds,
    IReadOnlyList<string> SubcontractorNames,
    bool MustChangePassword,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateUserRequest(
    string FullName,
    string Position,
    string Email,
    string UserType,
    Guid? SubcontractorId,
    string? ApproverRole,
    string? TansuRole,
    Guid? ManagerUserId,
    IReadOnlyList<Guid>? ProjectOids,
    IReadOnlyList<Guid>? SubcontractorIds);

public sealed record UpdateUserRequest(
    string FullName,
    string Position,
    bool IsActive,
    string? ApproverRole,
    string? TansuRole,
    Guid? ManagerUserId,
    IReadOnlyList<Guid>? ProjectOids,
    IReadOnlyList<Guid>? SubcontractorIds);

public sealed record CreateUserResponse(UserDto User, string? TemporaryPassword);
