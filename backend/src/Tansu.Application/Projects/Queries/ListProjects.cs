using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Projects.Queries;

public sealed record ProjectDto(Guid ProjectOid, string? Name, int SubcontractorsCount);

public sealed record ListProjectsQuery(string? Search) : IRequest<IReadOnlyList<ProjectDto>>;

public sealed class ListProjectsHandler(ITansuDbContext db)
    : IRequestHandler<ListProjectsQuery, IReadOnlyList<ProjectDto>>
{
    public async Task<IReadOnlyList<ProjectDto>> Handle(ListProjectsQuery req, CancellationToken ct)
    {
        var q = db.ProjectRefs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(p => p.Name != null && p.Name.ToLower().Contains(s));
        }

        return await q
            .OrderBy(p => p.Name)
            .Select(p => new ProjectDto(
                p.ProjectOid,
                p.Name,
                db.ProjectSubcontractors.Count(ps => ps.ProjectOid == p.ProjectOid)))
            .ToListAsync(ct);
    }
}

public sealed record RegisterProjectCommand(Guid ProjectOid, string? Name) : IRequest<ProjectDto>;

public sealed class RegisterProjectHandler(ITansuDbContext db)
    : IRequestHandler<RegisterProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(RegisterProjectCommand req, CancellationToken ct)
    {
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
        return new ProjectDto(existing.ProjectOid, existing.Name, 0);
    }
}
