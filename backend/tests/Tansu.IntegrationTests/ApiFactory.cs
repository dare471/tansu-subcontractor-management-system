using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Tansu.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tansu_test")
        .WithUsername("tansu")
        .WithPassword("tansu")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["RabbitMq:Host"] = "",
                ["Jwt:SigningKey"] = "test-signing-key-with-at-least-32-characters!!",
                ["Jwt:Issuer"] = "tansu-test",
                ["Jwt:Audience"] = "tansu-test-clients",
                ["App:PhotoStoragePath"] = Path.Combine(Path.GetTempPath(), "tansu-test-photos"),
                ["Entra:TenantId"] = "",
                ["Entra:Audience"] = ""
            });
        });
    }
}
