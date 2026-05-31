using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeeDocuments;

internal static class EmployeeBlockHelper
{
    internal static async Task<bool> IsBlockedAsync(
        ITansuDbContext db,
        Guid employeeId,
        CancellationToken ct)
    {
        var lastAction = await db.EmployeeBlockRecords.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.ActionType)
            .FirstOrDefaultAsync(ct);

        return lastAction == EmployeeBlockActionType.Block;
    }
}
