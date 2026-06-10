using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Zup;

namespace Tansu.Application.Projects.Queries;

public sealed record ProjectBindOptionDto(Guid ProjectOid, string? Code, string? Name);

public sealed record ListProjectBindOptionsQuery : IRequest<IReadOnlyList<ProjectBindOptionDto>>;

public sealed class ListProjectBindOptionsHandler(
    ITansuDbContext db,
    ITansuAccessService accessService,
    IZupProjectDirectory zupProjects) : IRequestHandler<ListProjectBindOptionsQuery, IReadOnlyList<ProjectBindOptionDto>>
{
    public async Task<IReadOnlyList<ProjectBindOptionDto>> Handle(
        ListProjectBindOptionsQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access,
            p => p.CanRegisterSubcontractors || p.CanManageProjects,
            "Справочник проектов для привязки недоступен для вашей роли.");

        await ZupProjectSync.SyncToLocalRefsAsync(db, zupProjects, ct);

        var q = db.ProjectRefs.AsNoTracking();
        var hasZupProjects = await q.AnyAsync(p => p.ZupId != null, ct);
        if (hasZupProjects)
            q = q.Where(p => p.ZupId != null);

        return await q
            .OrderBy(p => p.Code ?? p.Name)
            .Select(p => new ProjectBindOptionDto(p.ProjectOid, p.Code, p.Name))
            .ToListAsync(ct);
    }
}
