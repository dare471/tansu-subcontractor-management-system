using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

public class AuthFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
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

    private sealed record LoginPayload(
        string AccessToken,
        DateTimeOffset ExpiresAt,
        Guid UserId,
        string Email,
        string UserType,
        Guid? SubcontractorId,
        bool MustChangePassword);
}
