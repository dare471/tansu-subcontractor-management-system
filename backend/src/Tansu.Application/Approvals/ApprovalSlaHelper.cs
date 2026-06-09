using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Application.Approvals;

internal static class ApprovalSlaHelper
{
    public static async Task<(int? pendingDays, bool isEscalated)> ComputeAsync(
        ITansuDbContext db,
        DateTimeOffset? assignedAt,
        DateTimeOffset? escalatedAt,
        CancellationToken ct)
    {
        if (assignedAt is null)
            return (null, escalatedAt is not null);

        var days = (int)(DateTimeOffset.UtcNow - assignedAt.Value).TotalDays;
        return (days, escalatedAt is not null);
    }

    public static async Task<int> GetWarningDaysAsync(ITansuDbContext db, CancellationToken ct)
    {
        var policy = await db.ApprovalSlaPolicies.AsNoTracking()
            .Where(p => p.IsActive && p.Scope == "global")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);
        return policy?.PendingDaysWarning ?? 2;
    }
}
