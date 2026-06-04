using Microsoft.EntityFrameworkCore;
using Tansu.Domain.Entities;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

internal static class DemoSeederUploaders
{
    public static async Task<IReadOnlyDictionary<Guid, Guid>> LoadSubcontractorHrUserIdsAsync(
        TansuDbContext ctx, CancellationToken ct = default)
    {
        var montazh = await ctx.Subcontractors.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Bin == DemoSeedData.SubMontazhBin, ct);
        var energo = await ctx.Subcontractors.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Bin == DemoSeedData.SubEnergoBin, ct);

        var map = new Dictionary<Guid, Guid>();

        if (montazh is not null)
        {
            var userId = await ctx.Users.AsNoTracking()
                .Where(u => u.SubcontractorId == montazh.Id && u.UserType == Domain.Enums.UserType.Subcontractor)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct);
            if (userId != Guid.Empty)
                map[montazh.Id] = userId;
        }

        if (energo is not null)
        {
            var userId = await ctx.Users.AsNoTracking()
                .Where(u => u.SubcontractorId == energo.Id && u.UserType == Domain.Enums.UserType.Subcontractor)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct);
            if (userId != Guid.Empty)
                map[energo.Id] = userId;
        }

        return map;
    }

    public static void ApplyPhotoUploader(Employee employee, IReadOnlyDictionary<Guid, Guid> hrBySub)
    {
        if (employee.PhotoUploadedByUserId is not null)
            return;

        if (hrBySub.TryGetValue(employee.SubcontractorId, out var userId))
            employee.PhotoUploadedByUserId = userId;
    }
}
