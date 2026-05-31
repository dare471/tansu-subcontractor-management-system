using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests.Queries;

public sealed record GetDocumentRequestInboxQuery : IRequest<IReadOnlyList<DocumentRequestInboxItemDto>>;

public sealed class GetDocumentRequestInboxHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<GetDocumentRequestInboxQuery, IReadOnlyList<DocumentRequestInboxItemDto>>
{
    public async Task<IReadOnlyList<DocumentRequestInboxItemDto>> Handle(
        GetDocumentRequestInboxQuery req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var access = await accessService.GetAccessAsync(ct);

        var pending = await db.DocumentApprovalSheet.AsNoTracking()
            .Where(a => a.ApproverUserId == userId && a.Status == ApprovalStatus.Pending)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return Array.Empty<DocumentRequestInboxItemDto>();

        var byRound = pending.GroupBy(a => new { a.DocumentRequestId, a.RoundId });
        var items = new List<DocumentRequestInboxItemDto>();

        foreach (var grp in byRound)
        {
            var earliest = await db.DocumentApprovalSheet.AsNoTracking()
                .Where(a => a.DocumentRequestId == grp.Key.DocumentRequestId &&
                            a.RoundId == grp.Key.RoundId &&
                            a.Status == ApprovalStatus.Pending)
                .OrderBy(a => a.OrderNo)
                .FirstAsync(ct);

            if (grp.All(g => g.Id != earliest.Id))
                continue;

            var request = await db.DocumentRequests.AsNoTracking()
                .Include(r => r.Subcontractor)
                .Include(r => r.Project)
                .FirstAsync(r => r.Id == earliest.DocumentRequestId, ct);

            if (access.VisibleSubcontractorIds is { } subs && !subs.Contains(request.SubcontractorId))
                continue;
            if (access.VisibleProjectOids is { } projects && request.ProjectOid is { } poid && !projects.Contains(poid))
                continue;

            items.Add(new DocumentRequestInboxItemDto(
                earliest.Id, request.Id, request.RequestType, request.Title,
                request.Subcontractor!.Name, request.ProjectOid, request.Project?.Name,
                earliest.ApproverRole, earliest.OrderNo, earliest.CreatedAt));
        }

        return items.OrderByDescending(x => x.SubmittedAt).ToList();
    }
}
