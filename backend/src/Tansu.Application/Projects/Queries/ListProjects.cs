using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Projects;
using Tansu.Application.Zup;
using Tansu.Domain.Enums;

namespace Tansu.Application.Projects.Queries;

public sealed record ListProjectsQuery(string? Search) : IRequest<IReadOnlyList<ProjectDto>>;

public sealed class ListProjectsHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IZupProjectDirectory zupProjects) : IRequestHandler<ListProjectsQuery, IReadOnlyList<ProjectDto>>
{
    public async Task<IReadOnlyList<ProjectDto>> Handle(ListProjectsQuery req, CancellationToken ct)
    {
        if (currentUser.UserType == UserType.Tansu)
        {
            var access = await accessService.GetAccessAsync(ct);
            accessService.EnsurePermission(
                access, p => p.CanViewProjects, "Просмотр проектов недоступен для вашей роли.");
            await ZupProjectSync.SyncToLocalRefsAsync(db, zupProjects, ct);
        }

        var accessCtx = currentUser.UserType == UserType.Tansu
            ? await accessService.GetAccessAsync(ct)
            : null;

        var q = db.ProjectRefs.AsNoTracking();
        var hasZupProjects = await q.AnyAsync(p => p.ZupId != null, ct);
        if (hasZupProjects)
            q = q.Where(p => p.ZupId != null);

        if (accessCtx?.VisibleProjectOids is { } projectScope)
            q = q.Where(p => projectScope.Contains(p.ProjectOid));

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(p =>
                (p.Name != null && p.Name.ToLower().Contains(s)) ||
                (p.Code != null && p.Code.ToLower().Contains(s)) ||
                (p.Address != null && p.Address.ToLower().Contains(s)));
        }

        return await q
            .OrderBy(p => p.Code ?? p.Name)
            .Select(p => new ProjectDto(
                p.ProjectOid,
                p.ZupId,
                p.Code,
                p.Name,
                p.Address,
                db.ProjectSubcontractors.Count(ps => ps.ProjectOid == p.ProjectOid),
                p.ZupId != null))
            .ToListAsync(ct);
    }
}

public sealed record RegisterProjectCommand(Guid ProjectOid, string? Name) : IRequest<ProjectDto>;

public sealed class RegisterProjectHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IZupProjectDirectory zupProjects) : IRequestHandler<RegisterProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(RegisterProjectCommand req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Tansu)
            throw new ForbiddenException();

        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanManageProjects, "Регистрация проектов недоступна для вашей роли.");
        accessService.EnsureCanModify(access);

        await ZupProjectSync.SyncToLocalRefsAsync(db, zupProjects, ct);

        var existing = await db.ProjectRefs.FirstOrDefaultAsync(p => p.ProjectOid == req.ProjectOid, ct);
        if (existing is null)
        {
            existing = new Domain.Entities.ProjectRef { ProjectOid = req.ProjectOid, Name = req.Name };
            db.ProjectRefs.Add(existing);
        }
        else if (!string.IsNullOrWhiteSpace(req.Name))
        {
            existing.Name = req.Name;
        }

        await db.SaveChangesAsync(ct);
        return new ProjectDto(
            existing.ProjectOid,
            existing.ZupId,
            existing.Code,
            existing.Name,
            existing.Address,
            0,
            existing.ZupId != null);
    }
}
