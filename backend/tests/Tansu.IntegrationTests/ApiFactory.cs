using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Tansu.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtSigningKey = "test-signing-key-with-at-least-32-characters!!";
    private const string TestJwtIssuer = "tansu-test";
    private const string TestJwtAudience = "tansu-test-clients";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tansu_test")
        .WithUsername("tansu")
        .WithPassword("tansu")
        .Build();

    private string? _postgresConnectionString;

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("TZ", "UTC");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", TestJwtSigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtAudience);
        await _postgres.StartAsync();
        _postgresConnectionString = _postgres.GetConnectionString();
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgresConnectionString);
    }

    public new async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", null);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var connectionString = _postgresConnectionString ?? _postgres.GetConnectionString();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString,
                ["RabbitMq:Host"] = "",
                ["Jwt:SigningKey"] = TestJwtSigningKey,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["App:PhotoStoragePath"] = Path.Combine(Path.GetTempPath(), "tansu-test-photos"),
                ["Entra:TenantId"] = "",
                ["Entra:Audience"] = "",
                ["AccessPass:VerifyServiceKey"] = ApiTestContext.VerifyServiceKey,
                ["Zup:TenantId"] = "",
                ["Zup:ClientId"] = "",
                ["Zup:ClientSecret"] = ""
            });
        });
    }
}
