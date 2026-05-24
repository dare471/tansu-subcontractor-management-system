using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.DocumentRequests.Queries;

public sealed record ListDocumentMatricesQuery : IRequest<IReadOnlyList<DocumentMatrixSummaryDto>>;

public sealed class ListDocumentMatricesHandler(ITansuDbContext db)
    : IRequestHandler<ListDocumentMatricesQuery, IReadOnlyList<DocumentMatrixSummaryDto>>
{
    public async Task<IReadOnlyList<DocumentMatrixSummaryDto>> Handle(
        ListDocumentMatricesQuery req, CancellationToken ct)
    {
        var rows = await db.DocumentApprovalMatrix
            .AsNoTracking()
            .Include(m => m.Project)
            .Include(m => m.Subcontractor)
            .OrderBy(m => m.Project!.Name ?? m.ProjectOid.ToString())
            .ThenBy(m => m.Subcontractor!.Name)
            .ThenBy(m => m.RequestType)
            .ThenBy(m => m.OrderNo)
            .ToListAsync(ct);

        return rows
            .GroupBy(m => new { m.ProjectOid, m.SubcontractorId, m.RequestType })
            .Select(g =>
            {
                var first = g.First();
                return new DocumentMatrixSummaryDto(
                    g.Key.ProjectOid,
                    first.Project?.Name,
                    g.Key.SubcontractorId,
                    first.Subcontractor!.Name,
                    g.Key.RequestType,
                    g.OrderBy(m => m.OrderNo)
                        .Select(m => new DocumentMatrixStepDto(m.Id, m.OrderNo, m.ApproverRole))
                        .ToList());
            })
            .ToList();
    }
}
