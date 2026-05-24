using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Approvals.Queries;

public sealed record GetInboxQuery : IRequest<IReadOnlyList<InboxItemDto>>;

public sealed class GetInboxHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetInboxQuery, IReadOnlyList<InboxItemDto>>
{
    public async Task<IReadOnlyList<InboxItemDto>> Handle(GetInboxQuery req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var pending = await db.ApprovalSheet.AsNoTracking()
            .Where(a => a.ApproverUserId == userId && a.Status == ApprovalStatus.Pending)
            .ToListAsync(ct);

        if (pending.Count == 0) return Array.Empty<InboxItemDto>();

        var batchIds = pending
            .Where(a => a.BatchId.HasValue)
            .Select(a => a.BatchId!.Value)
            .Distinct()
            .ToList();

        var batches = batchIds.Count == 0
            ? new Dictionary<Guid, (string Title, DateTimeOffset SubmittedAt)>()
            : await db.EmployeeApprovalBatches.AsNoTracking()
                .Where(b => batchIds.Contains(b.Id))
                .ToDictionaryAsync(
                    b => b.Id,
                    b => (Title: b.Title, SubmittedAt: b.SubmittedAt ?? b.CreatedAt),
                    ct);

        var byRound = pending.GroupBy(a => new { a.EmployeeId, a.RoundId });
        var currentSteps = new List<InboxItemDto>();

        foreach (var grp in byRound)
        {
            var earliest = await db.ApprovalSheet.AsNoTracking()
                .Where(a => a.EmployeeId == grp.Key.EmployeeId &&
                            a.RoundId == grp.Key.RoundId &&
                            a.Status == ApprovalStatus.Pending)
                .OrderBy(a => a.OrderNo)
                .FirstAsync(ct);

            var mine = grp.FirstOrDefault(g => g.Id == earliest.Id);
            if (mine is null) continue;

            var employee = await db.Employees.AsNoTracking()
                .Include(e => e.Subcontractor)
                .Include(e => e.Project)
                .FirstAsync(e => e.Id == mine.EmployeeId, ct);

            string? batchTitle = null;
            Guid? batchId = mine.BatchId;
            var submittedAt = mine.CreatedAt;

            if (batchId is { } bid && batches.TryGetValue(bid, out var batchInfo))
            {
                batchTitle = batchInfo.Title;
                submittedAt = batchInfo.SubmittedAt;
            }

            currentSteps.Add(new InboxItemDto(
                mine.Id, employee.Id, employee.FullName, employee.Position,
                employee.SubcontractorId, employee.Subcontractor!.Name,
                employee.ProjectOid, employee.Project?.Name,
                mine.OrderNo, submittedAt, batchId, batchTitle));
        }

        return currentSteps
            .OrderByDescending(x => x.SubmittedAt)
            .ToList();
    }
}
