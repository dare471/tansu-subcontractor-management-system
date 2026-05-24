using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

public static class DemoApproversSeeder
{
    public const string DemoSubcontractorBin = DemoSeedData.SubMontazhBin;

    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>().CreateLogger("DemoApproversSeeder");

        var admin = await ctx.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == DemoSeedData.TansuAdminEmail.ToLower());
        if (admin is null) return;

        var approverUsers = new List<User>();
        foreach (var seed in DemoSeedData.TansuApprovers)
        {
            var email = seed.Email.ToLowerInvariant();
            var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user is null)
            {
                user = new User
                {
                    FullName = seed.FullName,
                    Position = seed.Position,
                    Email = email,
                    UserType = UserType.Tansu,
                    PasswordHash = null,
                    IsActive = true
                };
                ctx.Users.Add(user);
            }
            else if (user.FullName != seed.FullName || user.Position != seed.Position)
            {
                user.FullName = seed.FullName;
                user.Position = seed.Position;
            }

            approverUsers.Add(user);
        }

        await ctx.SaveChangesAsync();

        var chain = new[] { admin.Id, approverUsers[0].Id, approverUsers[1].Id, approverUsers[2].Id };
        var subs = await ctx.Subcontractors
            .Where(s => s.Bin == DemoSeedData.SubMontazhBin || s.Bin == DemoSeedData.SubEnergoBin)
            .ToListAsync();

        foreach (var sub in subs)
        {
            var bindings = await ctx.ProjectSubcontractors
                .Where(x => x.SubcontractorId == sub.Id)
                .Select(x => x.ProjectOid)
                .ToListAsync();

            foreach (var projectOid in bindings)
                await ApplyMatrixAsync(ctx, projectOid, sub.Id, chain, logger);
        }

        await ctx.SaveChangesAsync();
    }

    internal static async Task ApplyMatrixAsync(
        TansuDbContext ctx,
        Guid projectOid,
        Guid subcontractorId,
        IReadOnlyList<Guid> approverUserIds,
        ILogger logger)
    {
        var existing = await ctx.ApprovalMatrix
            .Where(m => m.ProjectOid == projectOid && m.SubcontractorId == subcontractorId)
            .OrderBy(m => m.OrderNo)
            .ToListAsync();

        var existingChain = existing.Select(e => e.UserId).ToList();
        if (existingChain.SequenceEqual(approverUserIds))
            return;

        foreach (var entry in existing)
            ctx.ApprovalMatrix.Remove(entry);

        for (var i = 0; i < approverUserIds.Count; i++)
        {
            ctx.ApprovalMatrix.Add(new ApprovalMatrixEntry
            {
                OrderNo = i + 1,
                ProjectOid = projectOid,
                SubcontractorId = subcontractorId,
                UserId = approverUserIds[i]
            });
        }

        logger.LogInformation(
            "Матрица согласования: проект {ProjectOid}, субподрядчик {SubcontractorId}",
            projectOid, subcontractorId);
    }
}
