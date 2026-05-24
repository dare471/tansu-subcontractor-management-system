using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests.Queries;

public sealed record ListDocumentRequestsQuery(string? RequestType, string? Search)
    : IRequest<IReadOnlyList<DocumentRequestDto>>;

public sealed class ListDocumentRequestsHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ListDocumentRequestsQuery, IReadOnlyList<DocumentRequestDto>>
{
    public async Task<IReadOnlyList<DocumentRequestDto>> Handle(ListDocumentRequestsQuery req, CancellationToken ct)
    {
        var q = db.DocumentRequests.AsNoTracking().AsQueryable();

        if (currentUser.UserType == UserType.Subcontractor)
        {
            var sid = currentUser.SubcontractorId
                ?? throw new ForbiddenException("Контекст субподрядчика отсутствует.");
            q = q.Where(x => x.SubcontractorId == sid);
        }

        if (!string.IsNullOrWhiteSpace(req.RequestType))
            q = q.Where(x => x.RequestType == req.RequestType);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(x => x.Title.ToLower().Contains(s) || x.Description.ToLower().Contains(s));
        }

        var list = await q
            .Include(x => x.Subcontractor)
            .Include(x => x.Project)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        if (list.Count == 0)
            return Array.Empty<DocumentRequestDto>();

        var ids = list.Select(x => x.Id).ToList();
        var sheets = await db.DocumentApprovalSheet.AsNoTracking()
            .Include(a => a.Approver)
            .Where(a => ids.Contains(a.DocumentRequestId))
            .ToListAsync(ct);

        var sheetsByRequest = sheets.GroupBy(s => s.DocumentRequestId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DocumentApprovalSheetEntry>)g.ToList());

        return list.Select(e =>
        {
            sheetsByRequest.TryGetValue(e.Id, out var requestSheets);
            requestSheets ??= Array.Empty<DocumentApprovalSheetEntry>();
            var (status, approverName, approverRole, stepNo) =
                DocumentRequestStatusResolver.Resolve(requestSheets);

            return new DocumentRequestDto(
                e.Id, e.SubcontractorId, e.Subcontractor!.Name,
                e.ProjectOid, e.Project!.Name,
                e.RequestType, e.Title, e.Description,
                status, approverName, approverRole, stepNo,
                e.CreatedAt, e.UpdatedAt);
        }).ToList();
    }
}
