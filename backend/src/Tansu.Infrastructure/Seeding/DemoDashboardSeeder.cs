using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Application.Employees;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

/// <summary>Демо-данные для сводки на странице «Отчёты».</summary>
public static class DemoDashboardSeeder
{
    private const string TerminalKeremet = "КПП Keremet — въезд";

    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DemoDashboardSeeder));

        if (!await ctx.Subcontractors.AnyAsync())
            return;

        var changed = false;
        changed |= await EnsureSiteVisitsTimelineAsync(ctx, logger);
        changed |= await EnsureOpenIncidentsAsync(ctx);
        changed |= await EnsureQuizCompletionsAsync(ctx);
        changed |= await EnsureActiveBlockAsync(ctx);

        if (changed)
            await ctx.SaveChangesAsync();
    }

    /// <summary>Проходы за последние 14 дней.</summary>
    private static async Task<bool> EnsureSiteVisitsTimelineAsync(TansuDbContext ctx, ILogger logger)
    {
        var employees = await ctx.Employees
            .Where(e => DemoSeedData.VisitJournalEmployeeIins.Contains(e.Iin))
            .ToListAsync();
        if (employees.Count == 0)
            return false;

        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddDays(-13).Date;
        var existing = await ctx.EmployeeSiteVisits
            .Where(v => employees.Select(e => e.Id).Contains(v.EmployeeId) && v.CheckedInAt >= windowStart)
            .Select(v => new { v.EmployeeId, Day = v.CheckedInAt.Date })
            .ToListAsync();

        var covered = existing
            .Select(x => (x.EmployeeId, x.Day))
            .ToHashSet();

        var added = 0;
        foreach (var employee in employees)
        {
            for (var day = 0; day < 14; day++)
            {
                var date = now.AddDays(-day).Date;
                if (covered.Contains((employee.Id, date)))
                    continue;

                var visitsToday = 1 + (day + employee.FullName.Length) % 3;
                for (var i = 0; i < visitsToday; i++)
                {
                    var checkIn = date.AddHours(7 + i).AddMinutes(15 * i);
                    ctx.EmployeeSiteVisits.Add(new EmployeeSiteVisit
                    {
                        EmployeeId = employee.Id,
                        CheckedInAt = checkIn,
                        CheckedOutAt = checkIn.AddHours(8),
                        TerminalLocation = TerminalKeremet,
                        FaceConfidence = 0.82 + i * 0.02,
                        VerificationMethod = "face_id",
                        DataSource = SiteVisitDataSource.FaceId
                    });
                    added++;
                }
            }
        }

        if (added > 0)
            logger.LogInformation("Демо-отчёты: добавлено {Count} проходов за 14 дней.", added);

        return added > 0;
    }

    private static async Task<bool> EnsureOpenIncidentsAsync(TansuDbContext ctx)
    {
        var openCount = await ctx.SiteIncidents.CountAsync(i => i.Status != "resolved");
        if (openCount >= 3)
            return false;

        var admin = await ctx.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == DemoSeedData.TansuAdminEmail.ToLower());
        if (admin is null)
            return false;

        var montazh = await ctx.Subcontractors.FirstOrDefaultAsync(s => s.Bin == DemoSeedData.SubMontazhBin);
        var templates = new[]
        {
            ("Нарушение ТБ на площадке", "Работник без каски у лифтового хола.", "low"),
            ("Повреждение ограждения", "Сбито временное ограждение у котлована.", "medium"),
            ("Утечка кабель-канала", "Повреждён кабель-канал на 2-м этаже.", "high"),
            ("Задымление в подсобке", "Запах гари в подсобном помещении.", "critical")
        };

        var added = false;
        foreach (var (title, desc, severity) in templates)
        {
            if (openCount >= 4) break;
            if (await ctx.SiteIncidents.AnyAsync(i => i.Title == title))
                continue;

            ctx.SiteIncidents.Add(new SiteIncident
            {
                ProjectOid = DemoSeedData.ProjectKeremetOid,
                SubcontractorId = montazh?.Id,
                ReportedByUserId = admin.Id,
                OccurredAt = DateTimeOffset.UtcNow.AddDays(-openCount - 1),
                Title = title,
                Description = desc,
                Severity = severity,
                Status = "open",
                BlockUntilResolved = severity is "high" or "critical"
            });
            openCount++;
            added = true;
        }

        return added;
    }

    private static async Task<bool> EnsureQuizCompletionsAsync(TansuDbContext ctx)
    {
        var employees = await ctx.Employees
            .Where(e => e.SubcontractorId != null)
            .ToListAsync();
        if (employees.Count == 0)
            return false;

        var sheets = await ctx.ApprovalSheet.AsNoTracking()
            .Where(a => employees.Select(e => e.Id).Contains(a.EmployeeId))
            .ToListAsync();
        var sheetsByEmployee = sheets.GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ApprovalSheetEntry>)g.ToList());

        var withQuiz = await ctx.EmployeeSafetyQuizCompletions
            .Select(q => q.EmployeeId)
            .ToHashSetAsync();

        var added = false;
        foreach (var employee in employees)
        {
            sheetsByEmployee.TryGetValue(employee.Id, out var empSheets);
            empSheets ??= Array.Empty<ApprovalSheetEntry>();
            if (EmployeeStatusResolver.ResolveFromSheets(empSheets) != ApprovalStatus.Approved)
                continue;
            if (withQuiz.Contains(employee.Id))
                continue;

            ctx.EmployeeSafetyQuizCompletions.Add(new EmployeeSafetyQuizCompletion
            {
                EmployeeId = employee.Id,
                Score = 4,
                TotalQuestions = 5,
                CompletedAt = DateTimeOffset.UtcNow.AddDays(-3)
            });
            added = true;
        }

        return added;
    }

    private static async Task<bool> EnsureActiveBlockAsync(TansuDbContext ctx)
    {
        var hasActiveBlock = await ctx.EmployeeBlockRecords.AnyAsync(b =>
            b.ActionType == EmployeeBlockActionType.Block &&
            b.Status == EmployeeBlockRequestStatus.Applied);
        if (hasActiveBlock)
            return false;

        var admin = await ctx.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == DemoSeedData.TansuAdminEmail.ToLower());
        var employee = await ctx.Employees
            .Where(e => e.Iin == DemoSeedData.SeedMontazhEmployeeIins[6])
            .FirstOrDefaultAsync();
        if (admin is null || employee is null)
            return false;

        ctx.EmployeeBlockRecords.Add(new EmployeeBlockRecord
        {
            EmployeeId = employee.Id,
            InitiatedByUserId = admin.Id,
            ActionType = EmployeeBlockActionType.Block,
            Reason = "Демо-блокировка для отчётов",
            Status = EmployeeBlockRequestStatus.Applied,
            InitiatorRole = TansuRole.GlobalAdmin,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        });

        return true;
    }
}
