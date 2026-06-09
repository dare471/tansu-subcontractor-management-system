using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

[Collection("ApiScenario")]
public sealed class AuditReportsIntegrationTests(ApiFactory factory)
{
    [Fact]
    public async Task AuditEvents_list_returns_paged_events()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);
        var res = await http.GetAsync("/api/audit-events?page=1&pageSize=20");
        res.EnsureSuccessStatusCode();

        var page = await res.Content.ReadFromJsonAsync<AuditPageDto>();
        page.Should().NotBeNull();
        page!.Items.Should().NotBeEmpty();
        page.Total.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BlockEmployee_writes_audit_event()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ITansuDbContext>();

        var employeeId = await FindApprovedUnblockedEmployeeIdAsync(db);
        if (employeeId is null)
            return;

        var res = await http.PostAsJsonAsync($"/api/employees/{employeeId}/block", new { reason = "Тест аудита блокировки" });
        res.EnsureSuccessStatusCode();

        var audit = await db.AuditEvents.AsNoTracking()
            .Where(e => e.Action == AuditActions.EmployeeBlocked && e.EntityId == employeeId)
            .OrderByDescending(e => e.OccurredAt)
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull();
    }

    [Fact]
    public async Task Reports_approved_personnel_export_returns_csv()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);
        var res = await http.GetAsync("/api/reports/approved-personnel/export?format=csv");
        res.EnsureSuccessStatusCode();
        res.Content.Headers.ContentType!.MediaType.Should().Contain("csv");
        (await res.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Reports_expiring_documents_export_works_without_daysAhead()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);
        var res = await http.GetAsync("/api/reports/expiring-documents/export?format=csv");
        res.EnsureSuccessStatusCode();
        res.Content.Headers.ContentType!.MediaType.Should().Contain("csv");
    }

    [Fact]
    public async Task Reports_site_visits_export_uses_canViewReports_not_visit_journal()
    {
        var http = factory.CreateClient();
        var login = await http.PostAsJsonAsync("/api/auth/dev-login",
            new { email = DemoSeedData.TansuApprovers[0].Email });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginDto>())!.AccessToken;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await http.GetAsync("/api/reports/site-visits/export?format=csv");
        res.EnsureSuccessStatusCode();
        res.Content.Headers.ContentType!.MediaType.Should().Contain("csv");
    }

    [Fact]
    public async Task Reports_subcontractor_compliance_returns_rows()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);
        var res = await http.GetAsync("/api/reports/subcontractor-compliance");
        res.EnsureSuccessStatusCode();

        var rows = await res.Content.ReadFromJsonAsync<ComplianceRowDto[]>();
        rows.Should().NotBeNull().And.NotBeEmpty();
        rows![0].SubcontractorName.Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<Guid?> FindApprovedUnblockedEmployeeIdAsync(ITansuDbContext db)
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
                    .AnyAsync(b => b.EmployeeId == id && b.Status == Domain.Enums.EmployeeBlockRequestStatus.Applied
                        && b.ActionType == Domain.Enums.EmployeeBlockActionType.Block))
                continue;
            return id;
        }

        return null;
    }

    private sealed record AuditPageDto(IReadOnlyList<AuditItemDto> Items, int Total, int Page, int PageSize);
    private sealed record AuditItemDto(string Action, string EntityType);
    private sealed record ComplianceRowDto(string SubcontractorName, int TotalEmployees);
}
