using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Application.AccessPasses.Commands;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Seeding;

/// <summary>
/// Выдаёт QR-пропуска сотрудникам, согласованным до появления функции пропусков.
/// </summary>
public static class DemoAccessPassesSeeder
{
    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ITansuDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemoAccessPassesSeeder");

        var employees = await db.Employees.AsNoTracking().Select(e => e.Id).ToListAsync();
        if (employees.Count == 0)
            return;

        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => employees.Contains(a.EmployeeId))
            .ToListAsync();

        var sheetsByEmployee = sheets
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ApprovalSheetEntry>)g.ToList());

        var withActivePass = await db.EmployeeAccessPasses.AsNoTracking()
            .Where(p => employees.Contains(p.EmployeeId) && p.RevokedAt == null)
            .Select(p => p.EmployeeId)
            .ToHashSetAsync();

        var issued = 0;
        foreach (var employeeId in employees)
        {
            if (withActivePass.Contains(employeeId))
                continue;

            sheetsByEmployee.TryGetValue(employeeId, out var employeeSheets);
            employeeSheets ??= Array.Empty<ApprovalSheetEntry>();
            if (EmployeeStatusResolver.ResolveFromSheets(employeeSheets) != Domain.Enums.ApprovalStatus.Approved)
                continue;

            await mediator.Send(new IssueEmployeeAccessPassCommand(employeeId));
            issued++;
        }

        if (issued > 0)
            logger.LogInformation("Выдано {Count} QR-пропусков для ранее согласованных сотрудников.", issued);
    }
}
