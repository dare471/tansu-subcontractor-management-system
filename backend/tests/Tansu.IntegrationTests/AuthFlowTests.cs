using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

public class AuthFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory = factory;
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task Login_with_seed_credentials_succeeds_and_returns_must_change_flag()
    {
        var res = await _http.PostAsJsonAsync("/api/auth/login", new
        {
            email = DemoSeeder.SubcontractorEmail,
            password = DemoSeeder.SubcontractorTempPassword
        });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<LoginPayload>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.MustChangePassword.Should().BeTrue();
        body.UserType.Should().Be("Subcontractor");
    }

    [Fact]
    public async Task Login_with_invalid_password_returns_401()
    {
        var res = await _http.PostAsJsonAsync("/api/auth/login", new
        {
            email = DemoSeeder.SubcontractorEmail,
            password = "nope"
        });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DevLogin_tansu_admin_succeeds_in_development()
    {
        var res = await _http.PostAsJsonAsync("/api/auth/dev-login", new
        {
            email = DemoSeeder.TansuAdminEmail
        });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<LoginPayload>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.UserType.Should().Be("TANSU");
        body.Email.Should().Be(DemoSeeder.TansuAdminEmail);
        body.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task DevLogin_rejects_subcontractor_user()
    {
        var res = await _http.PostAsJsonAsync("/api/auth/dev-login", new
        {
            email = DemoSeeder.SubcontractorEmail
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Change_password_issues_new_token_without_must_change_flag()
    {
        const string tempPassword = DemoSeeder.SubcontractorTempPassword;
        const string newPassword = "NewPass1";
        var email = $"pwd-{Guid.NewGuid():N}@tansu.local";

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var sub = await db.Subcontractors.FirstAsync();
        db.Users.Add(new User
        {
            FullName = "Тест смены пароля",
            Position = "Субподрядчик",
            Email = email,
            UserType = UserType.Subcontractor,
            SubcontractorId = sub.Id,
            PasswordHash = hasher.Hash(tempPassword),
            MustChangePassword = true,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var loginRes = await _http.PostAsJsonAsync("/api/auth/login", new { email, password = tempPassword });
        loginRes.EnsureSuccessStatusCode();
        var loginBody = await loginRes.Content.ReadFromJsonAsync<LoginPayload>();
        var oldToken = loginBody!.AccessToken;

        var blocked = await _http.SendAsync(new HttpRequestMessage(
            HttpMethod.Get, "/api/auth/me/projects")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", oldToken) }
        });
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var changeRes = await _http.SendAsync(new HttpRequestMessage(
            HttpMethod.Post, "/api/auth/change-password")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", oldToken) },
            Content = JsonContent.Create(new
            {
                oldPassword = tempPassword,
                newPassword
            })
        });
        changeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var changeBody = await changeRes.Content.ReadFromJsonAsync<LoginPayload>();
        changeBody!.AccessToken.Should().NotBeNullOrWhiteSpace();
        changeBody.AccessToken.Should().NotBe(oldToken);
        changeBody.MustChangePassword.Should().BeFalse();

        var allowed = await _http.SendAsync(new HttpRequestMessage(
            HttpMethod.Get, "/api/auth/me/projects")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", changeBody.AccessToken) }
        });
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record LoginPayload(
        string AccessToken,
        DateTimeOffset ExpiresAt,
        Guid UserId,
        string Email,
        string UserType,
        Guid? SubcontractorId,
        bool MustChangePassword);
}
