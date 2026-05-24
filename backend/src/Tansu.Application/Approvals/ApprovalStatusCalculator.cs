using Tansu.Domain.Enums;

namespace Tansu.Application.Approvals;

public static class ApprovalStatusCalculator
{
    /// <summary>
    /// Возвращает обобщённый статус цикла согласования по статусам отдельных шагов.
    /// </summary>
    public static string DetermineRoundStatus(IEnumerable<string> stepStatuses)
    {
        var list = stepStatuses?.ToList() ?? new List<string>();
        if (list.Count == 0) return "draft";
        if (list.Any(s => s == ApprovalStatus.Rejected)) return ApprovalStatus.Rejected;
        if (list.Any(s => s == ApprovalStatus.Pending)) return ApprovalStatus.Pending;
        if (list.All(s => s == ApprovalStatus.Approved)) return ApprovalStatus.Approved;
        return ApprovalStatus.Skipped;
    }
}
