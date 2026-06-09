using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Application.Common.Interfaces;
using Tansu.Infrastructure.Persistence;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

[Collection("ApiScenario")]
public sealed class EmployeePortalIntegrationTests(ApiFactory factory)
{
    private readonly ApiTestContext _ctx = new(factory);

    [Theory]
    [InlineData("en", "Must you use PPE")]
    [InlineData("kk", "ЖҚҚ")]
    [InlineData("ru", "средства индивидуальной защиты")]
    public async Task SafetyQuiz_returns_localized_questions(string locale, string expectedFragment)
    {
        var http = factory.CreateClient();
        var token = await _ctx.GetTokenAsync(ApiAuthKind.EmployeeOnly);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await http.GetAsync($"/api/employee-portal/safety-quiz?locale={locale}");
        res.EnsureSuccessStatusCode();

        var questions = await res.Content.ReadFromJsonAsync<QuizQuestionDto[]>();
        questions.Should().NotBeNull().And.NotBeEmpty();
        questions![0].Text.Should().Contain(expectedFragment, because: $"locale={locale}");
    }

    [Fact]
    public async Task SiteVisits_supports_pagination_and_terminal_fields()
    {
        var http = factory.CreateClient();
        var token = await _ctx.GetTokenAsync(ApiAuthKind.EmployeeOnly);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await http.GetAsync("/api/employee-portal/site-visits?page=1&pageSize=5");
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<SiteVisitsPageDto>();
        body.Should().NotBeNull();
        body!.Visits.Should().NotBeNull();
        body.Visits.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task SiteVisits_works_without_pagination_query()
    {
        var http = factory.CreateClient();
        var token = await _ctx.GetTokenAsync(ApiAuthKind.EmployeeOnly);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await http.GetAsync("/api/employee-portal/site-visits");
        res.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Dashboard_includes_pass_status_fields()
    {
        var http = factory.CreateClient();
        var token = await _ctx.GetTokenAsync(ApiAuthKind.EmployeeOnly);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await http.GetAsync("/api/employee-portal/dashboard");
        res.EnsureSuccessStatusCode();

        var dash = await res.Content.ReadFromJsonAsync<DashboardDto>();
        dash.Should().NotBeNull();
        dash!.EmployeeId.Should().NotBeEmpty();

        if (dash.AccessPass is not null)
        {
            dash.AccessPass.QrValidUntil.Should().NotBe(default);
            dash.AccessPass.PassStatus.Should().NotBeNullOrWhiteSpace();
            dash.AccessPass.EmployeeBlockStatus.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Employee_user_persists_notification_email()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var portalUser = await db.Users
            .FirstOrDefaultAsync(u => u.UserType == Domain.Enums.UserType.Employee && u.IsActive);

        portalUser.Should().NotBeNull();
        portalUser!.NotificationEmail = "notify-employee@test.kz";
        await db.SaveChangesAsync();

        var reloaded = await db.Users.AsNoTracking().FirstAsync(u => u.Id == portalUser.Id);
        reloaded.NotificationEmail.Should().Be("notify-employee@test.kz");
    }

    private sealed record QuizQuestionDto(string Id, string Text);
    private sealed record SiteVisitsPageDto(
        IReadOnlyList<SiteVisitItemDto> Visits,
        DateTimeOffset? LastCheckedInAt,
        int TotalCount);
    private sealed record SiteVisitItemDto(
        Guid Id,
        DateTimeOffset CheckedInAt,
        DateTimeOffset? CheckedOutAt,
        string? TerminalLocation);
    private sealed record DashboardDto(Guid EmployeeId, PassDto? AccessPass);
    private sealed record PassDto(
        DateTimeOffset QrValidUntil,
        string PassStatus,
        string EmployeeBlockStatus);
}
