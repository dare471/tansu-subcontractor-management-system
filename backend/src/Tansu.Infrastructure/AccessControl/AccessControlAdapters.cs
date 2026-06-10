using Microsoft.EntityFrameworkCore;
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

public sealed class HikAccessServiceBridge(
    IAccessControlOrchestrator orchestrator,
    ITansuDbContext db,
    IPhotoStorage photoStorage,
    ILogger<HikAccessServiceBridge> logger) : IHikAccessService
{
    public async Task GrantAccessAsync(Guid employeeId, CancellationToken ct)
    {
        try
        {
            var employee = await db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
            if (employee is null)
            {
                logger.LogWarning("СКУД: сотрудник {EmployeeId} не найден, заливка пропущена", employeeId);
                return;
            }

            var photoBytes = await ReadPhotoAsync(employee.PhotoPath, ct);
            var personCode = string.IsNullOrWhiteSpace(employee.Iin) ? null : employee.Iin.Trim();

            var person = new AccessControlPerson(
                employee.Id,
                employee.FullName,
                photoBytes,
                CardNumber: null,
                ValidFrom: DateTimeOffset.UtcNow,
                ValidTo: null,
                PersonCode: personCode,
                Phone: employee.Phone,
                Position: employee.Position);

            await orchestrator.SyncPersonAsync(person, employee.ProjectOid, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "СКУД: заливка сотрудника {EmployeeId} не выполнена", employeeId);
        }
    }

    public Task RevokeAccessAsync(Guid employeeId, string reason, CancellationToken ct) =>
        orchestrator.RevokePersonAsync(employeeId, reason, null, ct);

    private async Task<byte[]?> ReadPhotoAsync(string? photoPath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
            return null;

        await using var stream = await photoStorage.OpenReadAsync(photoPath, ct);
        if (stream is null)
            return null;

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
