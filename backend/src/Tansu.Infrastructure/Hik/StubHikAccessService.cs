using Microsoft.Extensions.Logging;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Infrastructure.Hik;

public sealed class StubHikAccessService(ILogger<StubHikAccessService> logger) : IHikAccessService
{
    public Task RevokeAccessAsync(Guid employeeId, string reason, CancellationToken ct)
    {
        logger.LogInformation(
            "HIK stub: revoke access for employee {EmployeeId}. Reason: {Reason}",
            employeeId, reason);
        return Task.CompletedTask;
    }

    public Task GrantAccessAsync(Guid employeeId, CancellationToken ct)
    {
        logger.LogInformation("HIK stub: grant access for employee {EmployeeId}", employeeId);
        return Task.CompletedTask;
    }
}
