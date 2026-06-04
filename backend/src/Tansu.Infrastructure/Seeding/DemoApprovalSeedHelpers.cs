using Microsoft.EntityFrameworkCore;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

internal static class DemoApprovalSeedHelpers
{
    public static async Task<Guid[]?> LoadEmployeeChainAsync(TansuDbContext ctx, CancellationToken ct = default)
    {
        var emails = new[] { DemoSeedData.TansuAdminEmail }
            .Concat(DemoSeedData.TansuApprovers.Select(a => a.Email))
            .ToArray();

        var chain = new List<Guid>();
        foreach (var email in emails)
        {
            var user = await ctx.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);
            if (user is null) return null;
            chain.Add(user.Id);
        }

        return chain.ToArray();
    }

    public static List<ApprovalSheetEntry> BuildEmployeeSheets(
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

    public static string ResolveStepStatus(
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
