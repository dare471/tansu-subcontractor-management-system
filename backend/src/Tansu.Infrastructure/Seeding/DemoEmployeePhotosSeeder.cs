using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

public static class DemoEmployeePhotosSeeder
{
    private static readonly string[] DemoSubBins =
    [
        DemoSeedData.SubMontazhBin,
        DemoSeedData.SubEnergoBin
    ];

    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IPhotoStorage>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemoEmployeePhotosSeeder");

        var subs = await ctx.Subcontractors.AsNoTracking()
            .Where(s => DemoSubBins.Contains(s.Bin))
            .Select(s => s.Id)
            .ToListAsync();

        if (subs.Count == 0)
            return;

        var employees = await ctx.Employees
            .Where(e => subs.Contains(e.SubcontractorId))
            .ToListAsync();

        if (employees.Count == 0)
            return;

        var portrait = DemoPortraitAsset.Bytes;
        var hrBySub = await DemoSeederUploaders.LoadSubcontractorHrUserIdsAsync(ctx);
        var changed = 0;

        foreach (var employee in employees)
        {
            if (DemoSeedData.PendingPhotoReviewIins.Contains(employee.Iin))
                continue;

            if (employee.PhotoReviewStatus == EmployeePhotoReviewStatus.Approved
                && !string.IsNullOrEmpty(employee.PhotoPath))
                continue;

            await using var stream = new MemoryStream(portrait);
            var relativePath = await storage.SaveAsync(employee.Id, "photo.jpg", stream, CancellationToken.None);

            employee.PhotoPath = relativePath;
            employee.PhotoReviewStatus = EmployeePhotoReviewStatus.Approved;
            employee.PhotoReviewReason = null;
            employee.UpdatedAt = DateTimeOffset.UtcNow;
            DemoSeederUploaders.ApplyPhotoUploader(employee, hrBySub);

            var hasReview = await ctx.EmployeePhotoReviews.AnyAsync(
                r => r.EmployeeId == employee.Id && r.PhotoPath == relativePath);

            if (!hasReview)
            {
                ctx.EmployeePhotoReviews.Add(new EmployeePhotoReview
                {
                    EmployeeId = employee.Id,
                    PhotoPath = relativePath,
                    ReviewType = EmployeePhotoReviewType.Manual,
                    Result = EmployeePhotoReviewResult.Passed,
                    Reason = "Начальные данные.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            changed++;
        }

        if (changed > 0)
        {
            await ctx.SaveChangesAsync();
            logger.LogInformation("Загружено фото для {Count} сотрудников.", changed);
        }
    }
}
