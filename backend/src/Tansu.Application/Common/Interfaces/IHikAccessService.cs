namespace Tansu.Application.Common.Interfaces;

public interface IHikAccessService
{
    Task RevokeAccessAsync(Guid employeeId, string reason, CancellationToken ct);
    Task GrantAccessAsync(Guid employeeId, CancellationToken ct);
}
