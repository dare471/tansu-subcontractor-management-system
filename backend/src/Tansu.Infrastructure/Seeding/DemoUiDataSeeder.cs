using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeePortal.Commands;
using Tansu.Application.Employees;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

public static class DemoUiDataSeeder
{
    private static readonly string[] AllSeedIins =
        DemoSeedData.SeedMontazhEmployeeIins.Concat(DemoSeedData.SeedEnergoEmployeeIins).ToArray();

    private const string TerminalKeremetIn = "КПП Keremet — въезд";
    private const string TerminalKeremetOut = "КПП Keremet — выезд";
    private const string TerminalAbay = "КПП Abay Tower";

    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IPhotoStorage>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DemoUiDataSeeder));

        if (!await ctx.Employees.AnyAsync(e => AllSeedIins.Contains(e.Iin)))
            return;

        var changed = false;
        changed |= await EnsureSubcontractorManagersAsync(ctx);
        changed |= await EnsurePendingPhotoReviewsAsync(ctx, storage);
        changed |= await EnsurePhotoUploadersAsync(ctx);
        changed |= await EnsureAdminApprovalInboxAsync(ctx);
        await EnsureEmployeePortalsAsync(ctx, mediator, logger);
        await EnsureSiteVisitsAsync(ctx, logger);

        if (changed)
            await ctx.SaveChangesAsync();
    }

    private static async Task<bool> EnsureSubcontractorManagersAsync(TansuDbContext ctx)
    {
        var subs = await ctx.Subcontractors
            .Where(s => s.Bin == DemoSeedData.SubMontazhBin || s.Bin == DemoSeedData.SubEnergoBin)
            .ToListAsync();

        var changed = false;
        foreach (var sub in subs)
        {
            if (sub.ManagerUserId is not null || sub.RegisteredByUserId is null)
                continue;

            sub.ManagerUserId = sub.RegisteredByUserId;
            changed = true;
        }

        return changed;
    }

    private static async Task<bool> EnsurePendingPhotoReviewsAsync(
        TansuDbContext ctx,
        IPhotoStorage storage)
    {
        var employees = await ctx.Employees
            .Where(e => DemoSeedData.PendingPhotoReviewIins.Contains(e.Iin))
            .ToListAsync();

        if (employees.Count == 0)
            return false;

        var portrait = DemoPortraitAsset.Bytes;
        var hrBySub = await DemoSeederUploaders.LoadSubcontractorHrUserIdsAsync(ctx);
        var changed = false;

        foreach (var employee in employees)
        {
            if (employee.PhotoReviewStatus == EmployeePhotoReviewStatus.Pending &&
                !string.IsNullOrEmpty(employee.PhotoPath) &&
                employee.PhotoUploadedByUserId is not null)
                continue;

            if (string.IsNullOrEmpty(employee.PhotoPath))
            {
                await using var stream = new MemoryStream(portrait);
                employee.PhotoPath = await storage.SaveAsync(
                    employee.Id, "photo.jpg", stream, CancellationToken.None);
            }

            employee.PhotoReviewStatus = EmployeePhotoReviewStatus.Pending;
            employee.PhotoReviewReason = "Ожидает ручной проверки.";
            employee.UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2);
            DemoSeederUploaders.ApplyPhotoUploader(employee, hrBySub);

            var hasPendingReview = await ctx.EmployeePhotoReviews.AnyAsync(r =>
                r.EmployeeId == employee.Id &&
                r.Result == EmployeePhotoReviewResult.Pending);

            if (!hasPendingReview)
            {
                ctx.EmployeePhotoReviews.Add(new EmployeePhotoReview
                {
                    EmployeeId = employee.Id,
                    PhotoPath = employee.PhotoPath!,
                    ReviewType = EmployeePhotoReviewType.Auto,
                    Result = EmployeePhotoReviewResult.Pending,
                    Reason = employee.PhotoReviewReason,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            changed = true;
        }

        return changed;
    }

    private static async Task<bool> EnsurePhotoUploadersAsync(TansuDbContext ctx)
    {
        var hrBySub = await DemoSeederUploaders.LoadSubcontractorHrUserIdsAsync(ctx);
        var employees = await ctx.Employees
            .Where(e => e.PhotoPath != null && e.PhotoUploadedByUserId == null)
            .ToListAsync();

        if (employees.Count == 0)
            return false;

        foreach (var employee in employees)
            DemoSeederUploaders.ApplyPhotoUploader(employee, hrBySub);

        return true;
    }

    private static async Task<bool> EnsureAdminApprovalInboxAsync(TansuDbContext ctx)
    {
        var admin = await ctx.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == DemoSeedData.TansuAdminEmail.ToLower());
        if (admin is null) return false;

        var aidos = await ctx.Employees.FirstOrDefaultAsync(e =>
            e.Iin == DemoSeedData.SeedMontazhEmployeeIins[0]);
        if (aidos is null) return false;

        var hasAdminPending = await ctx.ApprovalSheet.AnyAsync(a =>
            a.EmployeeId == aidos.Id &&
            a.ApproverUserId == admin.Id &&
            a.Status == ApprovalStatus.Pending);
        if (hasAdminPending)
            return false;

        var chain = await DemoApprovalSeedHelpers.LoadEmployeeChainAsync(ctx);
        if (chain is null) return false;

        var montazhUser = await ctx.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == DemoSeedData.SubMontazhEmail.ToLower());
        if (montazhUser is null) return false;

        var batch = await ctx.EmployeeApprovalBatches.FirstOrDefaultAsync(b =>
            b.Title == DemoSeedData.AdminInboxBatchTitle);
        if (batch is null)
        {
            batch = new EmployeeApprovalBatch
            {
                SubcontractorId = aidos.SubcontractorId,
                ProjectOid = aidos.ProjectOid,
                CreatedByUserId = montazhUser.Id,
                Title = DemoSeedData.AdminInboxBatchTitle,
                Status = BatchStatus.Submitted,
                EmployeeCount = 1,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
            };
            ctx.EmployeeApprovalBatches.Add(batch);
            await ctx.SaveChangesAsync();

            ctx.EmployeeApprovalBatchMembers.Add(new EmployeeApprovalBatchMember
            {
                BatchId = batch.Id,
                EmployeeId = aidos.Id,
                AddedAt = batch.SubmittedAt!.Value
            });
        }

        var oldSheets = await ctx.ApprovalSheet
            .Where(a => a.EmployeeId == aidos.Id)
            .ToListAsync();
        if (oldSheets.Count > 0)
            ctx.ApprovalSheet.RemoveRange(oldSheets);

        var submittedAt = batch.SubmittedAt ?? DateTimeOffset.UtcNow.AddDays(-1);
        var sheets = DemoApprovalSeedHelpers.BuildEmployeeSheets(
            aidos.Id, Guid.NewGuid(), batch.Id, chain,
            approvedThroughStep: 0, rejectedAtStep: null, submittedAt);
        ctx.ApprovalSheet.AddRange(sheets);

        return true;
    }

    private static async Task EnsureEmployeePortalsAsync(
        TansuDbContext ctx,
        IMediator mediator,
        ILogger logger)
    {
        var employees = await ctx.Employees.AsNoTracking().Select(e => e.Id).ToListAsync();
        var sheets = await ctx.ApprovalSheet.AsNoTracking()
            .Where(a => employees.Contains(a.EmployeeId))
            .ToListAsync();
        var sheetsByEmployee = sheets
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ApprovalSheetEntry>)g.ToList());

        var withPortal = await ctx.Users.AsNoTracking()
            .Where(u => u.EmployeeId != null)
            .Select(u => u.EmployeeId!.Value)
            .ToHashSetAsync();

        foreach (var employeeId in employees)
        {
            if (withPortal.Contains(employeeId))
                continue;

            sheetsByEmployee.TryGetValue(employeeId, out var employeeSheets);
            employeeSheets ??= Array.Empty<ApprovalSheetEntry>();
            if (EmployeeStatusResolver.ResolveFromSheets(employeeSheets) != ApprovalStatus.Approved)
                continue;

            if (await ctx.Employees.AsNoTracking()
                    .Where(e => e.Id == employeeId)
                    .Select(e => e.PhotoReviewStatus)
                    .FirstAsync() != EmployeePhotoReviewStatus.Approved)
                continue;

            await mediator.Send(new ProvisionEmployeePortalCommand(employeeId));
        }
    }

    private static async Task EnsureSiteVisitsAsync(TansuDbContext ctx, ILogger logger)
    {
        const int minPerEmployee = 5;

        var employees = await ctx.Employees
            .Where(e => DemoSeedData.VisitJournalEmployeeIins.Contains(e.Iin))
            .ToListAsync();

        if (employees.Count == 0)
            return;

        var employeeIds = employees.Select(e => e.Id).ToList();
        var visitCounts = await ctx.EmployeeSiteVisits
            .Where(v => employeeIds.Contains(v.EmployeeId))
            .GroupBy(v => v.EmployeeId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var passByEmployee = await ctx.EmployeeAccessPasses.AsNoTracking()
            .Where(p => employeeIds.Contains(p.EmployeeId) && p.RevokedAt == null)
            .GroupBy(p => p.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, PassId = g.OrderByDescending(p => p.IssuedAt).Select(p => p.Id).First() })
            .ToDictionaryAsync(x => x.EmployeeId, x => (Guid?)x.PassId);

        var now = DateTimeOffset.UtcNow;
        var added = 0;

        foreach (var employee in employees)
        {
            visitCounts.TryGetValue(employee.Id, out var existing);
            if (existing >= minPerEmployee)
                continue;

            passByEmployee.TryGetValue(employee.Id, out var passId);
            var isKeremet = employee.ProjectOid == DemoSeedData.ProjectKeremetOid;
            var terminalIn = isKeremet ? TerminalKeremetIn : TerminalAbay;
            var toAdd = minPerEmployee - existing;

            for (var i = 0; i < toAdd; i++)
            {
                var dayOffset = -(i % 5);
                var checkIn = now.AddDays(dayOffset).Date
                    .AddHours(7 + (employee.FullName.Length + i) % 3)
                    .AddMinutes(12 * i);
                var checkOut = checkIn.AddHours(8 + (i % 2));

                ctx.EmployeeSiteVisits.Add(new EmployeeSiteVisit
                {
                    EmployeeId = employee.Id,
                    AccessPassId = passId,
                    CheckedInAt = checkIn,
                    CheckedOutAt = dayOffset >= -3 ? checkOut : null,
                    TerminalLocation = terminalIn,
                    FaceConfidence = 0.84 + (i % 10) * 0.01,
                    VerificationMethod = "face_id",
                    DataSource = SiteVisitDataSource.FaceId
                });
                added++;

                if (dayOffset == 0 && isKeremet)
                {
                    ctx.EmployeeSiteVisits.Add(new EmployeeSiteVisit
                    {
                        EmployeeId = employee.Id,
                        AccessPassId = passId,
                        CheckedInAt = checkOut.AddMinutes(8),
                        TerminalLocation = TerminalKeremetOut,
                        FaceConfidence = 0.81,
                        VerificationMethod = "face_id",
                        DataSource = SiteVisitDataSource.HikTerminal
                    });
                    added++;
                }
            }
        }

        if (added > 0)
        {
            await ctx.SaveChangesAsync();
            logger.LogInformation("Добавлено записей в журнал посещений: {Count}.", added);
        }
    }
}
