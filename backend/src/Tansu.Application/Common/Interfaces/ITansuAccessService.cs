using Tansu.Application.Auth;

namespace Tansu.Application.Common.Interfaces;

public interface ITansuAccessService
{
    Task<TansuAccessContext> GetAccessAsync(CancellationToken ct);
    Task EnsureSubcontractorVisibleAsync(Guid subcontractorId, CancellationToken ct);
    Task EnsureEmployeeVisibleAsync(Guid employeeId, CancellationToken ct);
    void EnsurePermission(TansuAccessContext access, Func<TansuPermissionsDto, bool> check, string message);
}
