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
    string? BlockReason,
    DateTimeOffset CreatedAt);

public sealed record UserBlockRecordDto(
    Guid Id,
    Guid UserId,
    Guid InitiatedByUserId,
    string InitiatedByFullName,
    string ActionType,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record UserBlockStatusDto(
    bool IsBlocked,
    UserBlockRecordDto? LastRecord,
    IReadOnlyList<UserBlockRecordDto> History);

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
    string? StatusComment,
    string? ApproverRole,
    string? TansuRole,
    Guid? ManagerUserId,
    IReadOnlyList<Guid>? ProjectOids,
    IReadOnlyList<Guid>? SubcontractorIds);

public sealed record CreateUserResponse(UserDto User, string? TemporaryPassword);
