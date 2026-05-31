using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;
using Tansu.Infrastructure.Seeding;

namespace Tansu.Infrastructure.Seeding;

public static class DemoBlockRolesSeeder
{
    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemoBlockRolesSeeder");

        var assignments = new (string Email, string Role)[]
        {
            (DemoSeedData.TansuApprovers[0].Email, ApproverRole.OID),
            (DemoSeedData.TansuApprovers[1].Email, ApproverRole.Safety),
            ("security@tansu.local", ApproverRole.Security)
        };

        var changed = false;
        foreach (var (email, role) in assignments)
        {
            var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user is null && role == ApproverRole.Security)
            {
                user = new Domain.Entities.User
                {
                    Email = email,
                    FullName = "Ерлан Қасымов",
                    Position = "Служба безопасности",
                    UserType = UserType.Tansu,
                    ApproverRole = role,
                    IsActive = true
                };
                ctx.Users.Add(user);
                changed = true;
                continue;
            }

            if (user is null) continue;
            if (user.ApproverRole != role)
            {
                user.ApproverRole = role;
                changed = true;
            }
        }

        if (changed)
        {
            await ctx.SaveChangesAsync();
            logger.LogInformation("Роли блокировки (ОИД/БиОТ/СБ) обновлены.");
        }
    }
}
