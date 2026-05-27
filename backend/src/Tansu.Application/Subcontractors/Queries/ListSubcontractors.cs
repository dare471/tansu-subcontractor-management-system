using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Subcontractors.Queries;

public sealed record ListSubcontractorsQuery(string? Search) : IRequest<IReadOnlyList<SubcontractorDto>>;

public sealed class ListSubcontractorsHandler(ITansuDbContext db)
    : IRequestHandler<ListSubcontractorsQuery, IReadOnlyList<SubcontractorDto>>
{
    public async Task<IReadOnlyList<SubcontractorDto>> Handle(
        ListSubcontractorsQuery req, CancellationToken ct)
    {
        var q = db.Subcontractors.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(s) || x.Bin.Contains(s));
        }

        var subcontractors = await q
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Bin,
                ProjectsCount = x.Projects.Count,
                x.CreatedAt
            })
            .ToListAsync(ct);

        if (subcontractors.Count == 0)
            return Array.Empty<SubcontractorDto>();

        var subIds = subcontractors.Select(x => x.Id).ToList();
        var employees = await db.Employees.AsNoTracking()
            .Where(e => subIds.Contains(e.SubcontractorId))
            .Select(e => new { e.Id, e.SubcontractorId })
            .ToListAsync(ct);

        var approvalCounts = subIds.ToDictionary(id => id, _ => (Approved: 0, NotApproved: 0));

        if (employees.Count > 0)
        {
            var employeeIds = employees.Select(e => e.Id).ToList();
            var sheets = await db.ApprovalSheet.AsNoTracking()
                .Where(a => employeeIds.Contains(a.EmployeeId))
                .ToListAsync(ct);

            var sheetsByEmployee = sheets
                .GroupBy(s => s.EmployeeId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ApprovalSheetEntry>)g.ToList());

            foreach (var employee in employees)
            {
                sheetsByEmployee.TryGetValue(employee.Id, out var employeeSheets);
                employeeSheets ??= Array.Empty<ApprovalSheetEntry>();
                var status = EmployeeStatusResolver.ResolveFromSheets(employeeSheets);
                var counts = approvalCounts[employee.SubcontractorId];

                if (status == ApprovalStatus.Approved)
                    approvalCounts[employee.SubcontractorId] = (counts.Approved + 1, counts.NotApproved);
                else
                    approvalCounts[employee.SubcontractorId] = (counts.Approved, counts.NotApproved + 1);
            }
        }

        return subcontractors
            .Select(x =>
            {
                var counts = approvalCounts[x.Id];
                return new SubcontractorDto(
                    x.Id,
                    x.Name,
                    x.Bin,
                    x.ProjectsCount,
                    counts.Approved,
                    counts.NotApproved,
                    x.CreatedAt);
            })
            .ToList();
    }
}
