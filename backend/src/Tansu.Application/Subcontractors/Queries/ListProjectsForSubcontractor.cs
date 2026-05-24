using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Subcontractors.Queries;

public sealed record ListProjectsForSubcontractorQuery(Guid SubcontractorId)
    : IRequest<IReadOnlyList<ProjectBindingDto>>;

public sealed record ProjectBindingDto(Guid ProjectOid, string? Name);

public sealed class ListProjectsForSubcontractorHandler(ITansuDbContext db)
    : IRequestHandler<ListProjectsForSubcontractorQuery, IReadOnlyList<ProjectBindingDto>>
{
    public async Task<IReadOnlyList<ProjectBindingDto>> Handle(
        ListProjectsForSubcontractorQuery req, CancellationToken ct)
    {
        return await db.ProjectSubcontractors
            .AsNoTracking()
            .Where(x => x.SubcontractorId == req.SubcontractorId)
            .OrderBy(x => x.Project!.Name ?? x.ProjectOid.ToString())
            .Select(x => new ProjectBindingDto(x.ProjectOid, x.Project!.Name))
            .ToListAsync(ct);
    }
}
