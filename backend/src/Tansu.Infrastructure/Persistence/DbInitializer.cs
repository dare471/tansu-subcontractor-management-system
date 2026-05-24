using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tansu.Infrastructure.Persistence;

public static class DbInitializer
{
    /// <summary>
    /// Применяет начальную схему из embedded SQL-скрипта.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        logger.LogInformation("Применение схемы БД...");
        var sql = LoadEmbeddedSql("Tansu.Infrastructure.Persistence.Scripts.InitialSchema.sql");
        await ctx.Database.ExecuteSqlRawAsync(sql, ct);
        logger.LogInformation("Схема БД готова.");
    }

    private static string LoadEmbeddedSql(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. Available: {string.Join(", ", asm.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
