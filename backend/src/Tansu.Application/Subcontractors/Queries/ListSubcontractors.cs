using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Subcontractors.Queries;

public sealed record ListSubcontractorsQuery(string? Search) : IRequest<IReadOnlyList<SubcontractorDto>>;

public sealed class ListSubcontractorsHandler(ITansuDbContext db)
    : IRequestHandler<ListSubcontractorsQuery, IReadOnlyList<SubcontractorDto>>
{
    public async Task<IReadOnlyList<SubcontractorDto>> Handle(
        ListSubcontractorsQuery req, CancellationToken ct)
    {
        var q = db.Subcontractors.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(s) || x.Bin.Contains(s));
        }

        return await q
            .OrderBy(x => x.Name)
            .Select(x => new SubcontractorDto(
                x.Id, x.Name, x.Bin,
                x.Projects.Count,
                x.Users.Count,
                x.CreatedAt))
            .ToListAsync(ct);
    }
}
