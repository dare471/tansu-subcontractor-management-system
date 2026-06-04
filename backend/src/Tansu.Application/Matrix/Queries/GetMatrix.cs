using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Matrix.Queries;

public sealed record GetMatrixQuery(Guid ProjectOid, Guid SubcontractorId)
    : IRequest<IReadOnlyList<MatrixStepDto>>;

public sealed class GetMatrixHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<GetMatrixQuery, IReadOnlyList<MatrixStepDto>>
{
    public async Task<IReadOnlyList<MatrixStepDto>> Handle(GetMatrixQuery req, CancellationToken ct)
    {
        if (currentUser.UserType == UserType.Tansu)
        {
            var access = await accessService.GetAccessAsync(ct);
            accessService.EnsurePermission(
                access,
                p => p.CanManageApprovalMatrix || p.CanViewEmployees,
                "Просмотр матрицы согласования недоступен для вашей роли.");
            await accessService.EnsureSubcontractorVisibleAsync(req.SubcontractorId, ct);
            if (access.VisibleProjectOids is { } projects && !projects.Contains(req.ProjectOid))
                throw new Common.Exceptions.ForbiddenException("Проект вне вашей области видимости.");
        }

        return await db.ApprovalMatrix
            .AsNoTracking()
            .Where(m => m.ProjectOid == req.ProjectOid && m.SubcontractorId == req.SubcontractorId)
            .OrderBy(m => m.OrderNo)
            .Select(m => new MatrixStepDto(
                m.Id, m.OrderNo, m.UserId, m.User!.FullName, m.User.Email))
            .ToListAsync(ct);
    }
}
