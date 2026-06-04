using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

public static class DemoTansuRolesSeeder
{
    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemoTansuRolesSeeder");

        var changed = false;

        var admin = await ctx.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == DemoSeedData.TansuAdminEmail.ToLower());
        if (admin is not null)
        {
            if (admin.TansuRole != TansuRole.GlobalAdmin)
            {
                admin.TansuRole = TansuRole.GlobalAdmin;
                changed = true;
            }
        }

        var oidManager = await EnsureUserAsync(
            ctx, DemoSeedData.TansuApprovers[0].Email,
            DemoSeedData.TansuApprovers[0].FullName,
            DemoSeedData.TansuApprovers[0].Position,
            TansuRole.OidManager, ApproverRole.OID, null);
        changed |= oidManager.Changed;

        var safetyProject = await EnsureUserAsync(
            ctx, DemoSeedData.TansuApprovers[1].Email,
            DemoSeedData.TansuApprovers[1].FullName,
            DemoSeedData.TansuApprovers[1].Position,
            TansuRole.SafetyProject, ApproverRole.Safety, null);
        changed |= safetyProject.Changed;

        var oidDirector = await EnsureUserAsync(
            ctx, DemoSeedData.TansuApprovers[2].Email,
            DemoSeedData.TansuApprovers[2].FullName,
            DemoSeedData.TansuApprovers[2].Position,
            TansuRole.OidDirector, ApproverRole.Management, null);
        changed |= oidDirector.Changed;

        if (oidManager.User is not null && oidDirector.User is not null &&
            oidManager.User.ManagerUserId != oidDirector.User.Id)
        {
            oidManager.User.ManagerUserId = oidDirector.User.Id;
            changed = true;
        }

        var sbProject = await EnsureUserAsync(
            ctx, "security@tansu.local", "Ерлан Қасымов", "Служба безопасности",
            TansuRole.SbProject, ApproverRole.Security, null);
        changed |= sbProject.Changed;

        changed |= await EnsureProjectAssignmentAsync(ctx, safetyProject.User!.Id, DemoSeedData.ProjectKeremetOid);
        changed |= await EnsureProjectAssignmentAsync(ctx, sbProject.User!.Id, DemoSeedData.ProjectKeremetOid);

        if (oidManager.User is not null)
        {
            var montazh = await ctx.Subcontractors.FirstOrDefaultAsync(s => s.Bin == DemoSeedData.SubMontazhBin);
            if (montazh is not null)
            {
                if (montazh.RegisteredByUserId != oidManager.User.Id)
                {
                    montazh.RegisteredByUserId = oidManager.User.Id;
                    changed = true;
                }
                if (montazh.ManagerUserId != oidManager.User.Id)
                {
                    montazh.ManagerUserId = oidManager.User.Id;
                    changed = true;
                }
            }

            var energo = await ctx.Subcontractors.FirstOrDefaultAsync(s => s.Bin == DemoSeedData.SubEnergoBin);
            if (energo is not null)
            {
                if (energo.RegisteredByUserId != oidManager.User.Id)
                {
                    energo.RegisteredByUserId = oidManager.User.Id;
                    changed = true;
                }
                if (energo.ManagerUserId != oidManager.User.Id)
                {
                    energo.ManagerUserId = oidManager.User.Id;
                    changed = true;
                }
            }
        }

        foreach (var sub in await ctx.Subcontractors.ToListAsync())
        {
            if (!sub.IsActive)
            {
                sub.IsActive = true;
                changed = true;
            }
        }

        if (changed)
        {
            await ctx.SaveChangesAsync();
            logger.LogInformation("Роли ТАНСУ и области видимости обновлены.");
        }
    }

    private static async Task<(User? User, bool Changed)> EnsureUserAsync(
        TansuDbContext ctx,
        string email,
        string fullName,
        string position,
        string tansuRole,
        string approverRole,
        Guid? managerUserId)
    {
        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        var changed = false;

        if (user is null)
        {
            user = new User
            {
                Email = email,
                FullName = fullName,
                Position = position,
                UserType = UserType.Tansu,
                TansuRole = tansuRole,
                ApproverRole = approverRole,
                ManagerUserId = managerUserId,
                IsActive = true
            };
            ctx.Users.Add(user);
            return (user, true);
        }

        if (user.TansuRole != tansuRole) { user.TansuRole = tansuRole; changed = true; }
        if (user.ApproverRole != approverRole) { user.ApproverRole = approverRole; changed = true; }
        if (user.FullName != fullName) { user.FullName = fullName; changed = true; }
        if (user.Position != position) { user.Position = position; changed = true; }
        if (managerUserId is not null && user.ManagerUserId != managerUserId)
        {
            user.ManagerUserId = managerUserId;
            changed = true;
        }

        return (user, changed);
    }

    private static async Task<bool> EnsureProjectAssignmentAsync(
        TansuDbContext ctx, Guid userId, Guid projectOid)
    {
        if (await ctx.UserProjectAssignments.AnyAsync(a => a.UserId == userId && a.ProjectOid == projectOid))
            return false;

        ctx.UserProjectAssignments.Add(new UserProjectAssignment
        {
            UserId = userId,
            ProjectOid = projectOid
        });
        return true;
    }
}
