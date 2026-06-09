using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

[Collection("ApiScenario")]
public sealed class AuditLogTests(ApiFactory factory)
{
    [Fact]
    public async Task AuditEvents_list_is_available_for_admin()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);
        var res = await http.GetAsync("/api/audit-events?page=1&pageSize=10");
        res.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AuditEvents_list_works_without_pagination_query()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);
        var res = await http.GetAsync("/api/audit-events");
        res.EnsureSuccessStatusCode();
        var page = await res.Content.ReadFromJsonAsync<AuditPageDto>();
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
    }

    private sealed record AuditPageDto(int Total, int Page, int PageSize);

    [Fact]
    public async Task SiteVisitJournal_list_works_without_pagination_query()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);
        var res = await http.GetAsync("/api/site-visit-journal");
        res.EnsureSuccessStatusCode();
        var page = await res.Content.ReadFromJsonAsync<SiteVisitJournalPageDto>();
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
    }

    private sealed record SiteVisitJournalPageDto(int TotalCount, int Page, int PageSize);

    [Fact]
    public async Task ApproveEmployee_WritesAuditEvent()
    {
        var http = factory.CreateClient();
        var login = await http.PostAsJsonAsync("/api/auth/dev-login", new { email = DemoSeeder.TansuAdminEmail });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginDto>())!.AccessToken;
        http.DefaultRequestHeaders.Authorization = new("Bearer", token);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ITansuDbContext>();
        var pending = await db.ApprovalSheet.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Status == ApprovalStatus.Pending);
        if (pending is null) return;

        var res = await http.PostAsJsonAsync($"/api/approvals/{pending.Id}/approve", new { comment = (string?)null });
        res.EnsureSuccessStatusCode();

        var audit = await db.AuditEvents.AsNoTracking()
            .Where(e => e.Action == AuditActions.EmployeeApproved && e.EntityId == pending.EmployeeId)
            .OrderByDescending(e => e.OccurredAt)
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull();
    }

    private sealed record LoginDto(string AccessToken);
}
