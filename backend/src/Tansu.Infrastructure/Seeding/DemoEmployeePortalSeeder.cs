using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Application.EmployeePortal.Commands;
using Tansu.Application.Employees;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Seeding;

public static class DemoEmployeePortalSeeder
{
    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.ITansuDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemoEmployeePortalSeeder");

        var employees = await db.Employees.AsNoTracking().Select(e => e.Id).ToListAsync();
        if (employees.Count == 0)
            return;

        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => employees.Contains(a.EmployeeId))
            .ToListAsync();
        var sheetsByEmployee = sheets
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ApprovalSheetEntry>)g.ToList());

        var withPortal = await db.Users.AsNoTracking()
            .Where(u => u.EmployeeId != null)
            .Select(u => u.EmployeeId!.Value)
            .ToHashSetAsync();

        var provisioned = 0;
        foreach (var employeeId in employees)
        {
            if (withPortal.Contains(employeeId))
                continue;

            sheetsByEmployee.TryGetValue(employeeId, out var employeeSheets);
            employeeSheets ??= Array.Empty<ApprovalSheetEntry>();
            if (EmployeeStatusResolver.ResolveFromSheets(employeeSheets) != Domain.Enums.ApprovalStatus.Approved)
                continue;

            await mediator.Send(new ProvisionEmployeePortalCommand(employeeId));
            provisioned++;
        }

        if (provisioned > 0)
            logger.LogInformation("Создано {Count} личных кабинетов сотрудников.", provisioned);
    }
}
