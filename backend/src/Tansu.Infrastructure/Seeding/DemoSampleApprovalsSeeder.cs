using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

public static class DemoSampleApprovalsSeeder
{
    private sealed record EmployeeSample(
        string FullName, string Position, string Phone, string Iin,
        int ApprovedThroughStep, int? RejectedAtStep, bool InDraftBatch, bool InSubmittedBatch);

    private static readonly string[] AllSeedIins =
        DemoSeedData.SeedMontazhEmployeeIins.Concat(DemoSeedData.SeedEnergoEmployeeIins).ToArray();

    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>().CreateLogger("DemoSampleApprovalsSeeder");

        if (await ctx.Employees.AnyAsync(e => AllSeedIins.Contains(e.Iin)))
        {
            logger.LogInformation("Данные согласований уже в базе.");
            return;
        }

        await MigrateLegacySeedDataAsync(ctx, logger);

        var montazh = await ctx.Subcontractors.FirstOrDefaultAsync(s => s.Bin == DemoSeedData.SubMontazhBin);
        var montazhUser = await ctx.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == DemoSeeder.SubcontractorEmail.ToLower());
        if (montazh is null || montazhUser is null)
        {
            logger.LogInformation("Субподрядчик Montazh не найден.");
            return;
        }

        var employeeChain = await LoadEmployeeChainAsync(ctx);
        if (employeeChain is null) return;

        var roleApprovers = await LoadRoleApproversAsync(ctx);
        var baseTime = DateTimeOffset.UtcNow.AddDays(-14);

        var montazhSamples = new[]
        {
            new EmployeeSample("Айдос Нұрланов", "Монтажник", "+7 771 482 9156", DemoSeedData.SeedMontazhEmployeeIins[0], 0, null, false, false),
            new EmployeeSample("Гульмира Оразова", "Электрик", "+7 702 318 4472", DemoSeedData.SeedMontazhEmployeeIins[1], 0, null, true, false),
            new EmployeeSample("Ерлан Жумабеков", "Сварщик", "+7 747 903 2158", DemoSeedData.SeedMontazhEmployeeIins[2], 0, null, true, false),
            new EmployeeSample("Айгерим Сатова", "Инженер ПТО", "+7 705 661 8903", DemoSeedData.SeedMontazhEmployeeIins[3], 2, null, false, true),
            new EmployeeSample("Марат Бекенов", "Прораб", "+7 778 204 5591", DemoSeedData.SeedMontazhEmployeeIins[4], 3, null, false, true),
            new EmployeeSample("Сауле Касымова", "Мастер участка", "+7 701 556 7734", DemoSeedData.SeedMontazhEmployeeIins[5], 4, null, false, true),
            new EmployeeSample("Дана Куанова", "Крановщик", "+7 776 890 1247", DemoSeedData.SeedMontazhEmployeeIins[6], 1, 2, false, true),
        };

        var employees = new Dictionary<string, Employee>();
        foreach (var (sample, index) in montazhSamples.Select((s, i) => (s, i)))
        {
            var employee = new Employee
            {
                SubcontractorId = montazh.Id,
                ProjectOid = DemoSeedData.ProjectKeremetOid,
                FullName = sample.FullName,
                Position = sample.Position,
                Phone = sample.Phone,
                Iin = sample.Iin,
                CreatedAt = baseTime.AddDays(index),
                UpdatedAt = baseTime.AddDays(index)
            };
            ctx.Employees.Add(employee);
            employees[sample.Iin] = employee;
        }

        await ctx.SaveChangesAsync();

        var draftBatch = new EmployeeApprovalBatch
        {
            SubcontractorId = montazh.Id,
            ProjectOid = DemoSeedData.ProjectKeremetOid,
            CreatedByUserId = montazhUser.Id,
            Title = DemoSeedData.DraftBatchTitle,
            Status = BatchStatus.Draft,
            EmployeeCount = 2,
            CreatedAt = baseTime.AddDays(1)
        };
        ctx.EmployeeApprovalBatches.Add(draftBatch);

        foreach (var iin in DemoSeedData.SeedMontazhEmployeeIins.Skip(1).Take(2))
        {
            ctx.EmployeeApprovalBatchMembers.Add(new EmployeeApprovalBatchMember
            {
                BatchId = draftBatch.Id,
                EmployeeId = employees[iin].Id,
                AddedAt = baseTime.AddDays(1)
            });
        }

        var submittedBatch = new EmployeeApprovalBatch
        {
            SubcontractorId = montazh.Id,
            ProjectOid = DemoSeedData.ProjectKeremetOid,
            CreatedByUserId = montazhUser.Id,
            Title = DemoSeedData.SubmittedBatchTitle,
            Status = BatchStatus.Submitted,
            EmployeeCount = 4,
            CreatedAt = baseTime.AddDays(3),
            SubmittedAt = baseTime.AddDays(3).AddHours(2)
        };
        ctx.EmployeeApprovalBatches.Add(submittedBatch);

        foreach (var iin in DemoSeedData.SeedMontazhEmployeeIins.Skip(3))
        {
            ctx.EmployeeApprovalBatchMembers.Add(new EmployeeApprovalBatchMember
            {
                BatchId = submittedBatch.Id,
                EmployeeId = employees[iin].Id,
                AddedAt = baseTime.AddDays(3)
            });
        }

        await ctx.SaveChangesAsync();

        foreach (var sample in montazhSamples.Where(s => s.InSubmittedBatch))
        {
            var employee = employees[sample.Iin];
            var roundId = Guid.NewGuid();
            var sheets = BuildEmployeeSheets(
                employee.Id, roundId, submittedBatch.Id, employeeChain,
                sample.ApprovedThroughStep, sample.RejectedAtStep,
                baseTime.AddDays(3).AddHours(2));
            ctx.ApprovalSheet.AddRange(sheets);
        }

        if (roleApprovers is not null)
            await SeedDocumentRequestsAsync(ctx, montazh, montazhUser, baseTime, roleApprovers);

        var energo = await ctx.Subcontractors.FirstOrDefaultAsync(s => s.Bin == DemoSeedData.SubEnergoBin);
        var energoUser = await ctx.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == DemoSeedData.SubEnergoEmail.ToLower());
        if (energo is not null && energoUser is not null)
            await SeedEnergoEmployeesAsync(ctx, energo, energoUser, employeeChain, baseTime);

        await ctx.SaveChangesAsync();
        logger.LogInformation("Загружены согласования и пакеты.");
    }

    private static async Task MigrateLegacySeedDataAsync(TansuDbContext ctx, ILogger logger)
    {
        var legacyEmployees = await ctx.Employees
            .Where(e => DemoSeedData.LegacySeedIinPrefixes.Any(p => e.Iin.StartsWith(p)))
            .Select(e => e.Id)
            .ToListAsync();

        if (legacyEmployees.Count == 0) return;

        var legacySheets = await ctx.ApprovalSheet
            .Where(a => legacyEmployees.Contains(a.EmployeeId))
            .ToListAsync();
        ctx.ApprovalSheet.RemoveRange(legacySheets);

        var legacyMembers = await ctx.EmployeeApprovalBatchMembers
            .Where(m => legacyEmployees.Contains(m.EmployeeId))
            .ToListAsync();
        ctx.EmployeeApprovalBatchMembers.RemoveRange(legacyMembers);

        var legacyBatchIds = await ctx.EmployeeApprovalBatches
            .Where(b => b.Title.StartsWith("Демо:"))
            .Select(b => b.Id)
            .ToListAsync();
        var legacyBatches = await ctx.EmployeeApprovalBatches
            .Where(b => legacyBatchIds.Contains(b.Id))
            .ToListAsync();
        ctx.EmployeeApprovalBatches.RemoveRange(legacyBatches);

        var legacy = await ctx.Employees.Where(e => legacyEmployees.Contains(e.Id)).ToListAsync();
        ctx.Employees.RemoveRange(legacy);

        await ctx.SaveChangesAsync();
        logger.LogInformation("Удалены устаревшие записи.");
    }

    private static async Task SeedEnergoEmployeesAsync(
        TansuDbContext ctx,
        Subcontractor energo,
        User energoUser,
        Guid[] employeeChain,
        DateTimeOffset baseTime)
    {
        var energoSamples = new[]
        {
            ("Нурлан Төлеуов", "Электромонтажник", "+7 727 384 9021", DemoSeedData.SeedEnergoEmployeeIins[0], 0, (int?)null),
            ("Жанар Қасымова", "Инженер-сметчик", "+7 708 517 6634", DemoSeedData.SeedEnergoEmployeeIins[1], 0, (int?)null),
            ("Ерболат Сейтов", "Мастер СМР", "+7 775 290 4187", DemoSeedData.SeedEnergoEmployeeIins[2], 0, (int?)null),
        };

        var employees = new List<Employee>();
        foreach (var (name, pos, phone, iin, _, _) in energoSamples)
        {
            var e = new Employee
            {
                SubcontractorId = energo.Id,
                ProjectOid = DemoSeedData.ProjectAbayTowerOid,
                FullName = name,
                Position = pos,
                Phone = phone,
                Iin = iin,
                CreatedAt = baseTime.AddDays(20),
                UpdatedAt = baseTime.AddDays(20)
            };
            ctx.Employees.Add(e);
            employees.Add(e);
        }

        await ctx.SaveChangesAsync();

        var draftBatch = new EmployeeApprovalBatch
        {
            SubcontractorId = energo.Id,
            ProjectOid = DemoSeedData.ProjectAbayTowerOid,
            CreatedByUserId = energoUser.Id,
            Title = DemoSeedData.EnergoDraftBatchTitle,
            Status = BatchStatus.Draft,
            EmployeeCount = 2,
            CreatedAt = baseTime.AddDays(21)
        };
        ctx.EmployeeApprovalBatches.Add(draftBatch);

        foreach (var e in employees.Skip(1))
        {
            ctx.EmployeeApprovalBatchMembers.Add(new EmployeeApprovalBatchMember
            {
                BatchId = draftBatch.Id,
                EmployeeId = e.Id,
                AddedAt = baseTime.AddDays(21)
            });
        }
    }

    private static async Task SeedDocumentRequestsAsync(
        TansuDbContext ctx,
        Subcontractor sub,
        User subUser,
        DateTimeOffset baseTime,
        IReadOnlyDictionary<string, User> roleApprovers)
    {
        var documentSamples = new[]
        {
            (DocumentRequestType.Leave, "Отпуск 10–24 августа", "Ежегодный оплачиваемый отпуск.", 0, (int?)null, true),
            (DocumentRequestType.Leave, "Отпуск по семейным обстоятельствам", "С 3 по 7 июня.", 0, (int?)null, false),
            (DocumentRequestType.Ticket, "Командировка в Астану", "Совещание с генподрядчиком.", 1, (int?)null, false),
            (DocumentRequestType.Expense, "Закупка СИЗ для бригады", "Каски, страховочные пояса.", 3, (int?)null, false),
        };

        foreach (var (type, title, desc, approved, rejected, draft) in documentSamples)
        {
            var request = new DocumentRequest
            {
                SubcontractorId = sub.Id,
                ProjectOid = DemoSeedData.ProjectKeremetOid,
                CreatedByUserId = subUser.Id,
                RequestType = type,
                Title = title,
                Description = desc,
                CreatedAt = baseTime.AddDays(5),
                UpdatedAt = baseTime.AddDays(5)
            };
            ctx.DocumentRequests.Add(request);
            if (draft) continue;

            var chain = DocumentRoleChain(type, roleApprovers);
            if (chain.Count == 0) continue;

            var sheets = BuildDocumentSheets(
                request.Id, Guid.NewGuid(), chain, approved, rejected,
                baseTime.AddDays(5).AddHours(3));
            ctx.DocumentApprovalSheet.AddRange(sheets);
        }

        await Task.CompletedTask;
    }

    private static async Task<Guid[]?> LoadEmployeeChainAsync(TansuDbContext ctx)
    {
        var emails = new[] { DemoSeedData.TansuAdminEmail }
            .Concat(DemoSeedData.TansuApprovers.Select(a => a.Email))
            .ToArray();

        var chain = new List<Guid>();
        foreach (var email in emails)
        {
            var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user is null) return null;
            chain.Add(user.Id);
        }

        return chain.ToArray();
    }

    private static async Task<IReadOnlyDictionary<string, User>?> LoadRoleApproversAsync(TansuDbContext ctx)
    {
        var users = await ctx.Users
            .Where(u => u.UserType == UserType.Tansu && u.IsActive && u.ApproverRole != null)
            .ToListAsync();

        var map = new Dictionary<string, User>();
        foreach (var role in ApproverRole.All)
        {
            var user = users.FirstOrDefault(u => u.ApproverRole == role);
            if (user is null) return null;
            map[role] = user;
        }

        return map;
    }

    private static IReadOnlyList<(string Role, Guid ApproverId)> DocumentRoleChain(
        string requestType,
        IReadOnlyDictionary<string, User> roleApprovers)
    {
        string[] roles = requestType switch
        {
            DocumentRequestType.Leave => [ApproverRole.HR, ApproverRole.Management],
            DocumentRequestType.Ticket => [ApproverRole.HR, ApproverRole.Accounting],
            DocumentRequestType.Document or DocumentRequestType.Expense =>
                [ApproverRole.Accounting, ApproverRole.Finance, ApproverRole.Management],
            _ => []
        };

        return roles.Select(r => (r, roleApprovers[r].Id)).ToList();
    }

    private static List<ApprovalSheetEntry> BuildEmployeeSheets(
        Guid employeeId,
        Guid roundId,
        Guid batchId,
        IReadOnlyList<Guid> approverChain,
        int approvedThroughStep,
        int? rejectedAtStep,
        DateTimeOffset submittedAt)
    {
        var sheets = new List<ApprovalSheetEntry>();
        var decidedAt = submittedAt.AddHours(1);

        for (var i = 0; i < approverChain.Count; i++)
        {
            var orderNo = i + 1;
            var status = ResolveStepStatus(orderNo, approvedThroughStep, rejectedAtStep, approverChain.Count);
            var sheet = new ApprovalSheetEntry
            {
                EmployeeId = employeeId,
                ApproverUserId = approverChain[i],
                OrderNo = orderNo,
                RoundId = roundId,
                BatchId = batchId,
                Status = status,
                CreatedAt = submittedAt.AddMinutes(orderNo)
            };

            if (status is ApprovalStatus.Approved or ApprovalStatus.Rejected or ApprovalStatus.Skipped)
            {
                sheet.DecidedAt = decidedAt.AddMinutes(orderNo * 30);
                if (status == ApprovalStatus.Rejected)
                    sheet.Comment = "Не хватает документов для допуска на объект.";
            }

            sheets.Add(sheet);
        }

        return sheets;
    }

    private static List<DocumentApprovalSheetEntry> BuildDocumentSheets(
        Guid requestId,
        Guid roundId,
        IReadOnlyList<(string Role, Guid ApproverId)> chain,
        int approvedThroughStep,
        int? rejectedAtStep,
        DateTimeOffset submittedAt)
    {
        var sheets = new List<DocumentApprovalSheetEntry>();
        var decidedAt = submittedAt.AddHours(1);

        for (var i = 0; i < chain.Count; i++)
        {
            var orderNo = i + 1;
            var (role, approverId) = chain[i];
            var status = ResolveStepStatus(orderNo, approvedThroughStep, rejectedAtStep, chain.Count);
            sheets.Add(new DocumentApprovalSheetEntry
            {
                DocumentRequestId = requestId,
                ApproverUserId = approverId,
                ApproverRole = role,
                OrderNo = orderNo,
                RoundId = roundId,
                Status = status,
                CreatedAt = submittedAt.AddMinutes(orderNo),
                DecidedAt = status is ApprovalStatus.Approved or ApprovalStatus.Rejected or ApprovalStatus.Skipped
                    ? decidedAt.AddMinutes(orderNo * 45)
                    : null
            });
        }

        return sheets;
    }

    private static string ResolveStepStatus(
        int orderNo, int approvedThroughStep, int? rejectedAtStep, int chainLength)
    {
        if (rejectedAtStep is { } rejected)
        {
            if (orderNo < rejected) return ApprovalStatus.Approved;
            if (orderNo == rejected) return ApprovalStatus.Rejected;
            return ApprovalStatus.Skipped;
        }

        if (approvedThroughStep >= chainLength) return ApprovalStatus.Approved;
        if (orderNo <= approvedThroughStep) return ApprovalStatus.Approved;
        return ApprovalStatus.Pending;
    }
}
