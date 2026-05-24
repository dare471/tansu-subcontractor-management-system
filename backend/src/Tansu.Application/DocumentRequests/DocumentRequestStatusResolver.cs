using Tansu.Application.Approvals;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests;

internal static class DocumentRequestStatusResolver
{
    public static (string? Status, string? ApproverFullName, string? ApproverRole, int? StepNo)
        Resolve(IReadOnlyList<DocumentApprovalSheetEntry> sheets)
    {
        if (sheets.Count == 0)
            return (null, null, null, null);

        var latestRoundId = sheets
            .OrderByDescending(s => s.CreatedAt)
            .First()
            .RoundId;

        var roundSheets = sheets
            .Where(s => s.RoundId == latestRoundId)
            .OrderBy(s => s.OrderNo)
            .ToList();

        var roundStatus = ApprovalStatusCalculator.DetermineRoundStatus(roundSheets.Select(s => s.Status));
        if (roundStatus == "draft")
            return (null, null, null, null);

        if (roundStatus == ApprovalStatus.Pending)
        {
            var current = roundSheets.First(s => s.Status == ApprovalStatus.Pending);
            return (roundStatus, current.Approver?.FullName, current.ApproverRole, current.OrderNo);
        }

        if (roundStatus == ApprovalStatus.Rejected)
        {
            var rejected = roundSheets.First(s => s.Status == ApprovalStatus.Rejected);
            return (roundStatus, rejected.Approver?.FullName, rejected.ApproverRole, rejected.OrderNo);
        }

        return (roundStatus, null, null, null);
    }
}
