using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

public class UserCreateTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task Global_admin_creates_tansu_user_with_role()
    {
        var token = await DevLoginAsync(DemoSeeder.TansuAdminEmail);
        var email = $"tansu-{Guid.NewGuid():N}@tnsu.kz";

        var res = await _http.SendAsync(AuthorizedRequest(HttpMethod.Post, "/api/users", token, new
        {
            fullName = "Тест ТАНСУ",
            position = "Инженер",
            email,
            userType = UserType.Tansu,
            tansuRole = TansuRole.SbProject,
            employerCompany = TansuEmployerCompany.TansuConstruction
        }));

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<CreateUserPayload>();
        body!.User.Email.Should().Be(email.ToLowerInvariant());
        body.User.TansuRole.Should().Be(TansuRole.SbProject);
        body.TemporaryPassword.Should().BeNull();
    }

    [Fact]
    public async Task Global_admin_creates_subcontractor_admin_with_temp_password()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var subId = await ctx.Subcontractors.Select(s => s.Id).FirstAsync();

        var token = await DevLoginAsync(DemoSeeder.TansuAdminEmail);
        var email = $"sp-{Guid.NewGuid():N}@example.kz";

        var res = await _http.SendAsync(AuthorizedRequest(HttpMethod.Post, "/api/users", token, new
        {
            fullName = "HR Тест",
            position = "Менеджер",
            email,
            userType = UserType.Subcontractor,
            subcontractorId = subId
        }));

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<CreateUserPayload>();
        body!.TemporaryPassword.Should().NotBeNullOrWhiteSpace();
        body.User.UserType.Should().Be(UserType.Subcontractor);
    }

    [Fact]
    public async Task Global_admin_creates_tansu_user_with_project_assignments()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var projectOid = await ctx.ProjectRefs.Select(p => p.ProjectOid).FirstAsync();

        var token = await DevLoginAsync(DemoSeeder.TansuAdminEmail);
        var email = $"pm-{Guid.NewGuid():N}@tnsu.kz";

        var res = await _http.SendAsync(AuthorizedRequest(HttpMethod.Post, "/api/users", token, new
        {
            fullName = "Тест РП",
            position = "Руководитель проекта",
            email,
            userType = UserType.Tansu,
            tansuRole = TansuRole.ProjectManager,
            employerCompany = TansuEmployerCompany.TansuConstruction,
            projectOids = new[] { projectOid }
        }));

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<CreateUserPayload>();
        body!.User.ProjectOids.Should().Contain(projectOid);
    }

    [Fact]
    public async Task Manager_creates_subcontractor_admin_for_managed_org()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var managerEmail = DemoSeedData.TansuApprovers[0].Email;
        var subId = await ctx.Subcontractors
            .Where(s => s.ManagerUserId != null)
            .Select(s => s.Id)
            .FirstAsync();

        var token = await DevLoginAsync(managerEmail);
        var email = $"mgr-{Guid.NewGuid():N}@example.kz";

        var res = await _http.SendAsync(AuthorizedRequest(HttpMethod.Post, "/api/users", token, new
        {
            fullName = "Админ от менеджера",
            position = "HR",
            email,
            userType = UserType.Subcontractor,
            subcontractorId = subId
        }));

        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<string> DevLoginAsync(string email)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/dev-login", new { email });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<LoginPayload>();
        return body!.AccessToken;
    }

    private static HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, string token, object body)
    {
        var req = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private sealed record CreateUserPayload(CreateUserDto User, string? TemporaryPassword);

    private sealed record CreateUserDto(
        Guid Id,
        string Email,
        string UserType,
        string? TansuRole,
        IReadOnlyList<Guid> ProjectOids);
}
