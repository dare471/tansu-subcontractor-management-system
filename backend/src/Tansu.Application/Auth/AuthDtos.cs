namespace Tansu.Application.Auth;

public sealed record LoginRequest(string Email, string Password);
public sealed record DevLoginRequest(string Email);
public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string UserType,
    Guid? SubcontractorId,
    bool MustChangePassword,
    Guid? EmployeeId = null);

public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);

public sealed record MeResponse(
    Guid Id,
    string FullName,
    string Email,
    string Position,
    string UserType,
    Guid? SubcontractorId,
    string? SubcontractorName,
    string? SubcontractorBin,
    string? ApproverRole,
    bool MustChangePassword,
    Guid? EmployeeId = null);
