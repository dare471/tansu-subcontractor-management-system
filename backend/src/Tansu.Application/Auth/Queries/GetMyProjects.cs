using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Auth.Queries;

public sealed record MyProjectDto(
    Guid ProjectOid,
    string? Name,
    bool HasApprovalMatrix,
    string ActivityType,
    int CompletionPercent,
    DateTimeOffset? ProgressReportedAt);

public sealed record GetMyProjectsQuery : IRequest<IReadOnlyList<MyProjectDto>>;

public sealed class GetMyProjectsHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyProjectsQuery, IReadOnlyList<MyProjectDto>>
{
    public async Task<IReadOnlyList<MyProjectDto>> Handle(GetMyProjectsQuery request, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Subcontractor)
            throw new ForbiddenException("Список проектов доступен только пользователям субподрядчика.");

        var subcontractorId = currentUser.SubcontractorId
            ?? throw new ForbiddenException("Контекст субподрядчика отсутствует.");

        var bindings = await db.ProjectSubcontractors
            .AsNoTracking()
            .Where(x => x.SubcontractorId == subcontractorId)
            .OrderBy(x => x.Project!.Name ?? x.ProjectOid.ToString())
            .Select(x => new
            {
                x.ProjectOid,
                Name = x.Project!.Name,
                x.ActivityType,
                x.CompletionPercent,
                x.ProgressReportedAt
            })
            .ToListAsync(ct);

        if (bindings.Count == 0)
            return Array.Empty<MyProjectDto>();

        var withMatrix = await db.ApprovalMatrix
            .AsNoTracking()
            .Where(m => m.SubcontractorId == subcontractorId)
            .Select(m => m.ProjectOid)
            .Distinct()
            .ToListAsync(ct);

        var matrixSet = withMatrix.ToHashSet();

        return bindings
            .Select(b => new MyProjectDto(
                b.ProjectOid,
                b.Name,
                matrixSet.Contains(b.ProjectOid),
                b.ActivityType,
                b.CompletionPercent,
                b.ProgressReportedAt))
            .ToList();
    }
}
