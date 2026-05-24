using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Users.Queries;

public sealed record ListUsersQuery(string? UserType, Guid? SubcontractorId, string? Search)
    : IRequest<IReadOnlyList<UserDto>>;

public sealed class ListUsersHandler(ITansuDbContext db)
    : IRequestHandler<ListUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(ListUsersQuery req, CancellationToken ct)
    {
        var q = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.UserType))
            q = q.Where(u => u.UserType == req.UserType);

        if (req.SubcontractorId is { } sid)
            q = q.Where(u => u.SubcontractorId == sid);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(u => u.Email.ToLower().Contains(s) || u.FullName.ToLower().Contains(s));
        }

        return await q
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto(
                u.Id, u.FullName, u.Position, u.Email, u.UserType,
                u.SubcontractorId,
                u.Subcontractor != null ? u.Subcontractor.Name : null,
                u.ApproverRole,
                u.MustChangePassword, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
    }
}
