using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.IntegrationTests;

[Collection("ApiScenario")]
public sealed class AccessControlIntegrationTests(ApiFactory factory)
{
    [Fact]
    public async Task BlockEmployee_succeeds_with_scud_bridge_registered()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TansuDbContext>();

        var employeeId = await FindApprovedUnblockedEmployeeIdAsync(db);
        if (employeeId is null)
            return;

        var res = await http.PostAsJsonAsync($"/api/employees/{employeeId}/block", new
        {
            reason = "Тест СКУД-интеграции"
        });
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<BlockResultDto>();
        body.Should().NotBeNull();
        body!.ActionType.Should().Be("block");
    }

    [Fact]
    public void Access_control_adapters_are_registered()
    {
        using var scope = factory.Services.CreateScope();
        var systems = scope.ServiceProvider.GetServices<IAccessControlSystem>().ToList();
        systems.Should().NotBeEmpty();
        systems.Select(s => s.VendorId).Should().Contain("hik").And.Contain("perco").And.Contain("sigur");
    }

    private static async Task<Guid?> FindApprovedUnblockedEmployeeIdAsync(TansuDbContext db)
    {
        var employees = await db.Employees.AsNoTracking().Select(e => e.Id).ToListAsync();
        foreach (var id in employees)
        {
            var sheets = await db.ApprovalSheet.AsNoTracking()
                .Where(a => a.EmployeeId == id)
                .ToListAsync();
            if (EmployeeStatusResolver.ResolveFromSheets(sheets) != ApprovalStatus.Approved)
                continue;
            if (await db.EmployeeBlockRecords.AsNoTracking()
                    .AnyAsync(b => b.EmployeeId == id && b.Status == EmployeeBlockRequestStatus.Applied
                        && b.ActionType == EmployeeBlockActionType.Block))
                continue;
            return id;
        }

        return null;
    }

    private sealed record BlockResultDto(string ActionType);
}
