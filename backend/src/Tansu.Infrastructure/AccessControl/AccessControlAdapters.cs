using Microsoft.Extensions.Logging;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Infrastructure.AccessControl;

public sealed class AccessControlOrchestrator(
    IEnumerable<IAccessControlSystem> systems,
    ILogger<AccessControlOrchestrator> logger) : IAccessControlOrchestrator
{
    public async Task SyncPersonAsync(AccessControlPerson person, Guid? projectOid, CancellationToken ct)
    {
        foreach (var system in systems)
        {
            try
            {
                await system.SyncPersonAsync(person, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SCUD sync failed for vendor {Vendor}", system.VendorId);
            }
        }
    }

    public async Task RevokePersonAsync(Guid employeeId, string reason, Guid? projectOid, CancellationToken ct)
    {
        foreach (var system in systems)
        {
            try
            {
                await system.RevokePersonAsync(employeeId, reason, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SCUD revoke failed for vendor {Vendor}", system.VendorId);
            }
        }
    }
}

public abstract class StubAccessAdapter(string vendorId, ILogger logger) : IAccessControlSystem
{
    public string VendorId => vendorId;

    public Task SyncPersonAsync(AccessControlPerson person, CancellationToken ct)
    {
        logger.LogInformation("[{Vendor}] Sync person {EmployeeId}", vendorId, person.EmployeeId);
        return Task.CompletedTask;
    }

    public Task RevokePersonAsync(Guid employeeId, string reason, CancellationToken ct)
    {
        logger.LogInformation("[{Vendor}] Revoke {EmployeeId}: {Reason}", vendorId, employeeId, reason);
        return Task.CompletedTask;
    }

    public Task<bool> IsHealthyAsync(CancellationToken ct) => Task.FromResult(true);
}

public sealed class HikvisionAccessAdapter(ILogger<HikvisionAccessAdapter> logger)
    : StubAccessAdapter("hik", logger);

public sealed class PerCoAccessAdapter(ILogger<PerCoAccessAdapter> logger)
    : StubAccessAdapter("perco", logger);

public sealed class SigurAccessAdapter(ILogger<SigurAccessAdapter> logger)
    : StubAccessAdapter("sigur", logger);

public sealed class HikAccessServiceBridge(IAccessControlOrchestrator orchestrator) : IHikAccessService
{
    public Task GrantAccessAsync(Guid employeeId, CancellationToken ct) =>
        orchestrator.SyncPersonAsync(new AccessControlPerson(employeeId, "", null, null, null, null), null, ct);

    public Task RevokeAccessAsync(Guid employeeId, string reason, CancellationToken ct) =>
        orchestrator.RevokePersonAsync(employeeId, reason, null, ct);
}
