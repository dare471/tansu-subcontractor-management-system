using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

public static class DemoProjectDetailsSeeder
{
    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemoProjectDetailsSeeder");

        var admin = await ctx.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == DemoSeedData.TansuAdminEmail.ToLower());
        var pm = await ctx.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == DemoSeedData.TansuApprovers[0].Email.ToLower());

        var changed = false;

        changed |= await PatchProjectAsync(ctx, DemoSeedData.ProjectKeremetOid,
            "АО «BI Group Development»", "+7 717 244 0000", "projects@bi-group.kz",
            450_000_000m, admin?.Id, pm?.Id);

        changed |= await PatchProjectAsync(ctx, DemoSeedData.ProjectAbayTowerOid,
            "ТОО «Almaty Business Center»", "+7 727 350 8800", "office@abaytower.kz",
            280_000_000m, admin?.Id, pm?.Id);

        if (changed)
        {
            await ctx.SaveChangesAsync();
            logger.LogInformation("Карточки проектов обновлены.");
        }
    }

    private static async Task<bool> PatchProjectAsync(
        TansuDbContext ctx,
        Guid projectOid,
        string customerName,
        string customerPhone,
        string customerEmail,
        decimal budget,
        Guid? adminId,
        Guid? pmId)
    {
        var project = await ctx.ProjectRefs.FirstOrDefaultAsync(p => p.ProjectOid == projectOid);
        if (project is null) return false;

        var changed = false;
        if (project.CustomerName != customerName) { project.CustomerName = customerName; changed = true; }
        if (project.CustomerPhone != customerPhone) { project.CustomerPhone = customerPhone; changed = true; }
        if (project.CustomerEmail != customerEmail) { project.CustomerEmail = customerEmail; changed = true; }
        if (project.BudgetAmount != budget) { project.BudgetAmount = budget; changed = true; }
        if (project.BudgetCurrency != "KZT") { project.BudgetCurrency = "KZT"; changed = true; }
        if (adminId is not null && project.ResponsibleAdminUserId != adminId)
        {
            project.ResponsibleAdminUserId = adminId;
            changed = true;
        }
        if (pmId is not null && project.ProjectManagerUserId != pmId)
        {
            project.ProjectManagerUserId = pmId;
            changed = true;
        }

        return changed;
    }
}
