using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Matrix.Queries;

public sealed record ListMatricesQuery : IRequest<IReadOnlyList<MatrixSummaryDto>>;

public sealed class ListMatricesHandler(ITansuDbContext db)
    : IRequestHandler<ListMatricesQuery, IReadOnlyList<MatrixSummaryDto>>
{
    public async Task<IReadOnlyList<MatrixSummaryDto>> Handle(ListMatricesQuery req, CancellationToken ct)
    {
        var rows = await db.ApprovalMatrix
            .AsNoTracking()
            .Include(m => m.Project)
            .Include(m => m.Subcontractor)
            .Include(m => m.User)
            .OrderBy(m => m.Project!.Name ?? m.ProjectOid.ToString())
            .ThenBy(m => m.Subcontractor!.Name)
            .ThenBy(m => m.OrderNo)
            .ToListAsync(ct);

        return rows
            .GroupBy(m => new { m.ProjectOid, m.SubcontractorId })
            .Select(g =>
            {
                var first = g.First();
                return new MatrixSummaryDto(
                    g.Key.ProjectOid,
                    first.Project?.Name,
                    g.Key.SubcontractorId,
                    first.Subcontractor!.Name,
                    g.OrderBy(m => m.OrderNo)
                        .Select(m => new MatrixStepDto(
                            m.Id, m.OrderNo, m.UserId,
                            m.User!.FullName, m.User.Email))
                        .ToList());
            })
            .ToList();
    }
}
