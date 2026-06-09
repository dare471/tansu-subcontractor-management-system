using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Approvals.Queries;

public sealed record GetInboxQuery : IRequest<IReadOnlyList<InboxItemDto>>;

public sealed class GetInboxHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<GetInboxQuery, IReadOnlyList<InboxItemDto>>
{
    public async Task<IReadOnlyList<InboxItemDto>> Handle(GetInboxQuery req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var access = await accessService.GetAccessAsync(ct);

        var batchIds = await db.ApprovalSheet.AsNoTracking()
            .Where(a => a.Status == ApprovalStatus.Pending &&
                        (a.ApproverUserId == userId || a.ActingForUserId == userId) &&
                        a.BatchId != null)
            .Select(a => a.BatchId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var batches = batchIds.Count == 0
            ? new Dictionary<Guid, (string Title, DateTimeOffset SubmittedAt)>()
            : await db.EmployeeApprovalBatches.AsNoTracking()
                .Where(b => batchIds.Contains(b.Id))
                .ToDictionaryAsync(
                    b => b.Id,
                    b => (Title: b.Title, SubmittedAt: b.SubmittedAt ?? b.CreatedAt),
                    ct);

        var roundKeys = await db.ApprovalSheet.AsNoTracking()
            .Where(a => a.Status == ApprovalStatus.Pending &&
                        (a.ApproverUserId == userId || a.ActingForUserId == userId))
            .Select(a => new { a.EmployeeId, a.RoundId })
            .Distinct()
            .ToListAsync(ct);

        if (roundKeys.Count == 0) return Array.Empty<InboxItemDto>();

        var currentSteps = new List<InboxItemDto>();

        foreach (var key in roundKeys)
        {
            var earliest = await db.ApprovalSheet.AsNoTracking()
                .Where(a => a.EmployeeId == key.EmployeeId &&
                            a.RoundId == key.RoundId &&
                            a.Status == ApprovalStatus.Pending)
                .OrderBy(a => a.OrderNo)
                .FirstAsync(ct);

            if (earliest.ApproverUserId != userId && earliest.ActingForUserId != userId)
                continue;

            var mine = earliest;

            var employee = await db.Employees.AsNoTracking()
                .Include(e => e.Subcontractor)
                .Include(e => e.Project)
                .FirstAsync(e => e.Id == mine.EmployeeId, ct);

            if (!IsEmployeeVisible(access, employee.SubcontractorId, employee.ProjectOid))
                continue;

            string? batchTitle = null;
            Guid? batchId = mine.BatchId;
            var submittedAt = mine.CreatedAt;

            if (batchId is { } bid && batches.TryGetValue(bid, out var batchInfo))
            {
                batchTitle = batchInfo.Title;
                submittedAt = batchInfo.SubmittedAt;
            }

            var (pendingDays, isEscalated) = await ApprovalSlaHelper.ComputeAsync(
                db, mine.AssignedAt ?? mine.CreatedAt, mine.EscalatedAt, ct);
            string? actingForName = null;
            if (mine.ActingForUserId is Guid delegatorId)
            {
                actingForName = await db.Users.AsNoTracking()
                    .Where(u => u.Id == delegatorId)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync(ct);
            }

            currentSteps.Add(new InboxItemDto(
                mine.Id, employee.Id, employee.FullName, employee.Position,
                employee.SubcontractorId, employee.Subcontractor!.Name,
                employee.ProjectOid, employee.Project?.Name,
                mine.OrderNo, submittedAt, batchId, batchTitle,
                pendingDays, isEscalated, actingForName,
                mine.ApproverUserId == userId));
        }

        return currentSteps
            .OrderByDescending(x => x.SubmittedAt)
            .ToList();
    }

    private static bool IsEmployeeVisible(TansuAccessContext access, Guid subcontractorId, Guid projectOid)
    {
        if (access.VisibleSubcontractorIds is { } subs && !subs.Contains(subcontractorId))
            return false;
        if (access.VisibleProjectOids is { } projects && !projects.Contains(projectOid))
            return false;
        return true;
    }
}
