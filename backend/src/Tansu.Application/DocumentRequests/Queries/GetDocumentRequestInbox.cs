using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Approvals;
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

        var roundKeys = await db.DocumentApprovalSheet.AsNoTracking()
            .Where(a => a.Status == ApprovalStatus.Pending &&
                        (a.ApproverUserId == userId || a.ActingForUserId == userId))
            .Select(a => new { a.DocumentRequestId, a.RoundId })
            .Distinct()
            .ToListAsync(ct);

        if (roundKeys.Count == 0)
            return Array.Empty<DocumentRequestInboxItemDto>();

        var items = new List<DocumentRequestInboxItemDto>();

        foreach (var key in roundKeys)
        {
            var earliest = await db.DocumentApprovalSheet.AsNoTracking()
                .Where(a => a.DocumentRequestId == key.DocumentRequestId &&
                            a.RoundId == key.RoundId &&
                            a.Status == ApprovalStatus.Pending)
                .OrderBy(a => a.OrderNo)
                .FirstAsync(ct);

            if (earliest.ApproverUserId != userId && earliest.ActingForUserId != userId)
                continue;
            var request = await db.DocumentRequests.AsNoTracking()
                .Include(r => r.Subcontractor)
                .Include(r => r.Project)
                .FirstAsync(r => r.Id == earliest.DocumentRequestId, ct);

            if (access.VisibleSubcontractorIds is { } subs && !subs.Contains(request.SubcontractorId))
                continue;
            if (access.VisibleProjectOids is { } projects && request.ProjectOid is { } poid && !projects.Contains(poid))
                continue;

            var (pendingDays, isEscalated) = await ApprovalSlaHelper.ComputeAsync(
                db, earliest.AssignedAt ?? earliest.CreatedAt, earliest.EscalatedAt, ct);

            items.Add(new DocumentRequestInboxItemDto(
                earliest.Id, request.Id, request.RequestType, request.Title,
                request.Subcontractor!.Name, request.ProjectOid, request.Project?.Name,
                earliest.ApproverRole, earliest.OrderNo, earliest.CreatedAt,
                pendingDays, isEscalated,
                earliest.ApproverUserId == userId));
        }

        return items.OrderByDescending(x => x.SubmittedAt).ToList();
    }
}
