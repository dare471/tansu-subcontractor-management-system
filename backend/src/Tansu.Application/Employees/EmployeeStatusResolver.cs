using Tansu.Application.Approvals;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Employees;

public static class EmployeeStatusResolver
{
    public static string? ResolveFromSheets(IReadOnlyList<ApprovalSheetEntry> sheets)
    {
        if (sheets.Count == 0)
            return null;

        var latestRoundId = sheets
            .OrderByDescending(s => s.CreatedAt)
            .First()
            .RoundId;

        var roundSheets = sheets
            .Where(s => s.RoundId == latestRoundId)
            .ToList();

        var roundStatus = ApprovalStatusCalculator.DetermineRoundStatus(roundSheets.Select(s => s.Status));
        return roundStatus == "draft" ? null : roundStatus;
    }
}
