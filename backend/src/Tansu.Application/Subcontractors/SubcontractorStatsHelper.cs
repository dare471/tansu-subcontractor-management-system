using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Subcontractors;

internal static class SubcontractorStatsHelper
{
    public static async Task<(int Approved, int NotApproved)> CountEmployeesAsync(
        ITansuDbContext db, Guid subcontractorId, CancellationToken ct)
    {
        var employeeIds = await db.Employees.AsNoTracking()
            .Where(e => e.SubcontractorId == subcontractorId)
            .Select(e => e.Id)
            .ToListAsync(ct);

        if (employeeIds.Count == 0)
            return (0, 0);

        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId))
            .ToListAsync(ct);

        var sheetsByEmployee = sheets
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ApprovalSheetEntry>)g.ToList());

        var approved = 0;
        var notApproved = 0;
        foreach (var employeeId in employeeIds)
        {
            sheetsByEmployee.TryGetValue(employeeId, out var employeeSheets);
            employeeSheets ??= Array.Empty<ApprovalSheetEntry>();
            if (EmployeeStatusResolver.ResolveFromSheets(employeeSheets) == ApprovalStatus.Approved)
                approved++;
            else
                notApproved++;
        }

        return (approved, notApproved);
    }

    public static async Task<SubcontractorDto> ToDtoAsync(
        ITansuDbContext db,
        Subcontractor entity,
        CancellationToken ct)
    {
        var projectsCount = entity.Projects?.Count
            ?? await db.ProjectSubcontractors.CountAsync(p => p.SubcontractorId == entity.Id, ct);

        var (approved, notApproved) = await CountEmployeesAsync(db, entity.Id, ct);

        string? managerName = null;
        if (entity.ManagerUserId is { } mid)
        {
            managerName = entity.Manager?.FullName
                ?? await db.Users.AsNoTracking()
                    .Where(u => u.Id == mid)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync(ct);
        }

        return SubcontractorMapper.ToDto(entity, projectsCount, approved, notApproved, managerName);
    }
}
