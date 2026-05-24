using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Matrix.Queries;

public sealed record GetMatrixQuery(Guid ProjectOid, Guid SubcontractorId)
    : IRequest<IReadOnlyList<MatrixStepDto>>;

public sealed class GetMatrixHandler(ITansuDbContext db)
    : IRequestHandler<GetMatrixQuery, IReadOnlyList<MatrixStepDto>>
{
    public async Task<IReadOnlyList<MatrixStepDto>> Handle(GetMatrixQuery req, CancellationToken ct)
    {
        return await db.ApprovalMatrix
            .AsNoTracking()
            .Where(m => m.ProjectOid == req.ProjectOid && m.SubcontractorId == req.SubcontractorId)
            .OrderBy(m => m.OrderNo)
            .Select(m => new MatrixStepDto(
                m.Id, m.OrderNo, m.UserId, m.User!.FullName, m.User.Email))
            .ToListAsync(ct);
    }
}
