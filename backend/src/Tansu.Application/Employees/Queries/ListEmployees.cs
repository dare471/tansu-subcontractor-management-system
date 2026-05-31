using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeeDocuments;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Employees.Queries;

public sealed record ListEmployeesQuery(
    Guid? ProjectOid,
    Guid? SubcontractorId,
    string? Search) : IRequest<IReadOnlyList<EmployeeDto>>;

public sealed class ListEmployeesHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<ListEmployeesQuery, IReadOnlyList<EmployeeDto>>
{
    public async Task<IReadOnlyList<EmployeeDto>> Handle(ListEmployeesQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        if (currentUser.UserType == UserType.Tansu)
        {
            accessService.EnsurePermission(
                access, p => p.CanViewEmployees, "Нет доступа к сотрудникам.");
        }

        var q = db.Employees.AsNoTracking().AsQueryable();

        if (currentUser.UserType == UserType.Subcontractor)
        {
            var sid = currentUser.SubcontractorId
                ?? throw new ForbiddenException("Контекст субподрядчика отсутствует.");
            q = q.Where(e => e.SubcontractorId == sid);
        }
        else if (access.VisibleSubcontractorIds is { } scopeSubs)
        {
            q = q.Where(e => scopeSubs.Contains(e.SubcontractorId));
        }

        if (access.VisibleProjectOids is { } scopeProjects)
            q = q.Where(e => scopeProjects.Contains(e.ProjectOid));

        if (req.SubcontractorId is { } reqSid)
        {
            if (access.VisibleSubcontractorIds is { } visible && !visible.Contains(reqSid))
                throw new ForbiddenException("Субподрядчик вне вашей области видимости.");
            q = q.Where(e => e.SubcontractorId == reqSid);
        }

        if (req.ProjectOid is { } poid)
        {
            if (access.VisibleProjectOids is { } projects && !projects.Contains(poid))
                throw new ForbiddenException("Проект вне вашей области видимости.");
            q = q.Where(e => e.ProjectOid == poid);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(e => e.FullName.ToLower().Contains(s) || e.Iin.Contains(s));
        }

        var list = await q
            .Include(e => e.Subcontractor)
            .Include(e => e.Project)
            .OrderBy(e => e.FullName)
            .ToListAsync(ct);

        if (list.Count == 0)
            return Array.Empty<EmployeeDto>();

        var ids = list.Select(e => e.Id).ToList();
        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeId))
            .ToListAsync(ct);

        var sheetsByEmployee = sheets
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ApprovalSheetEntry>)g.ToList());

        var memberRows = await db.EmployeeApprovalBatchMembers.AsNoTracking()
            .Where(m => ids.Contains(m.EmployeeId))
            .Join(
                db.EmployeeApprovalBatches.AsNoTracking(),
                m => m.BatchId,
                b => b.Id,
                (m, b) => new { m.EmployeeId, b.Id, b.Title, b.Status })
            .ToListAsync(ct);

        var draftByEmployee = memberRows
            .Where(x => x.Status == BatchStatus.Draft)
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First());

        var submittedBatchIds = sheets
            .Where(s => s.BatchId.HasValue)
            .Select(s => s.BatchId!.Value)
            .Distinct()
            .ToList();

        var submittedBatches = submittedBatchIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.EmployeeApprovalBatches.AsNoTracking()
                .Where(b => submittedBatchIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Title, ct);

        var blockRows = await db.EmployeeBlockRecords.AsNoTracking()
            .Where(r => ids.Contains(r.EmployeeId))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.EmployeeId, r.ActionType, r.Reason })
            .ToListAsync(ct);

        var lastBlockByEmployee = blockRows
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First());

        return list.Select(e =>
        {
            sheetsByEmployee.TryGetValue(e.Id, out var employeeSheets);
            employeeSheets ??= Array.Empty<ApprovalSheetEntry>();
            var status = EmployeeStatusResolver.ResolveFromSheets(employeeSheets);

            Guid? draftBatchId = null;
            string? draftBatchTitle = null;
            if (draftByEmployee.TryGetValue(e.Id, out var draft))
            {
                draftBatchId = draft.Id;
                draftBatchTitle = draft.Title;
            }

            Guid? submittedBatchId = null;
            string? submittedBatchTitle = null;
            var latestBatchId = employeeSheets
                .Where(s => s.BatchId.HasValue)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => s.BatchId)
                .FirstOrDefault();
            if (latestBatchId is { } sbid && submittedBatches.TryGetValue(sbid, out var stitle))
            {
                submittedBatchId = sbid;
                submittedBatchTitle = stitle;
            }

            var isBlocked = false;
            string? blockReason = null;
            if (lastBlockByEmployee.TryGetValue(e.Id, out var blockInfo))
            {
                isBlocked = blockInfo.ActionType == EmployeeBlockActionType.Block;
                blockReason = isBlocked ? blockInfo.Reason : null;
            }

            return new EmployeeDto(
                e.Id, e.SubcontractorId, e.Subcontractor!.Name,
                e.ProjectOid, e.Project!.Name,
                e.FullName, e.Position, e.Phone, e.Iin, e.PhotoPath,
                e.PhotoReviewStatus, e.PhotoReviewReason,
                isBlocked, blockReason,
                status, draftBatchId, draftBatchTitle, submittedBatchId, submittedBatchTitle,
                e.CreatedAt, e.UpdatedAt);
        }).ToList();
    }
}
