namespace Tansu.Application.Common.Interfaces;

public interface IEmployeePortalCredentialWriter
{
    Task WriteAsync(
        Guid employeeId,
        string fullName,
        string iin,
        string oneTimePassword,
        CancellationToken cancellationToken = default);
}
