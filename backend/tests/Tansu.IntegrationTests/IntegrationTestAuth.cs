using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

internal static class IntegrationTestAuth
{
    internal static async Task<HttpClient> LoginAdminAsync(ApiFactory factory)
    {
        var http = factory.CreateClient();
        var login = await http.PostAsJsonAsync("/api/auth/dev-login", new { email = DemoSeeder.TansuAdminEmail });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginDto>())!.AccessToken;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    private sealed record LoginDto(string AccessToken);
}
