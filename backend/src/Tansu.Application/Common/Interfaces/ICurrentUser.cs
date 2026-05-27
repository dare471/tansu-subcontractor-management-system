namespace Tansu.Application.Common.Interfaces;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Email { get; }
    string? UserType { get; }
    Guid? SubcontractorId { get; }
    Guid? EmployeeId { get; }
    bool MustChangePassword { get; }
}
