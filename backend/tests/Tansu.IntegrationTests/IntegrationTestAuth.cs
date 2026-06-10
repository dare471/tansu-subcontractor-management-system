using System.Net.Http.Headers;
using System.Net.Http.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeePortal.Commands;
using Tansu.Application.Employees;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

internal static class IntegrationTestAuth
{
    internal const string EmployeeTestPassword = "EmployeeTest1!";

    internal static async Task<HttpClient> LoginAdminAsync(ApiFactory factory)
    {
        var http = factory.CreateClient();
        var login = await http.PostAsJsonAsync("/api/auth/dev-login", new { email = DemoSeeder.TansuAdminEmail });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginDto>())!.AccessToken;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    internal static async Task<string> GetEmployeeAccessTokenAsync(ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var portalUser = await db.Users
            .Where(u => u.UserType == UserType.Employee && u.EmployeeId != null)
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync();

        if (portalUser is null)
        {
            var employees = await db.Employees.AsNoTracking().Select(e => e.Id).ToListAsync();
            var sheets = await db.ApprovalSheet.AsNoTracking()
                .Where(a => employees.Contains(a.EmployeeId))
                .ToListAsync();
            var sheetsByEmployee = sheets.GroupBy(s => s.EmployeeId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ApprovalSheetEntry>)g.ToList());

            foreach (var employeeId in employees)
            {
                sheetsByEmployee.TryGetValue(employeeId, out var employeeSheets);
                employeeSheets ??= Array.Empty<ApprovalSheetEntry>();
                if (EmployeeStatusResolver.ResolveFromSheets(employeeSheets) != ApprovalStatus.Approved)
                    continue;

                await mediator.Send(new ProvisionEmployeePortalCommand(employeeId));
                portalUser = await db.Users.FirstAsync(
                    u => u.EmployeeId == employeeId && u.UserType == UserType.Employee);
                break;
            }
        }

        if (portalUser is null)
            throw new InvalidOperationException("В демо-данных нет согласованного сотрудника с личным кабинетом.");

        portalUser.IsActive = true;
        portalUser.MustChangePassword = false;
        portalUser.PasswordHash = hasher.Hash(EmployeeTestPassword);
        await db.SaveChangesAsync();

        var employeeIin = await db.Employees
            .Where(e => e.Id == portalUser.EmployeeId)
            .Select(e => e.Iin)
            .FirstAsync();

        var loginClient = factory.CreateClient();
        var login = await loginClient.PostAsJsonAsync("/api/auth/employee/login", new
        {
            iin = employeeIin,
            password = EmployeeTestPassword
        });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<LoginDto>())!.AccessToken;
    }

    internal static async Task<HttpClient> LoginEmployeeAsync(ApiFactory factory)
    {
        var http = factory.CreateClient();
        var token = await GetEmployeeAccessTokenAsync(factory);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    private sealed record LoginDto(string AccessToken);
}
