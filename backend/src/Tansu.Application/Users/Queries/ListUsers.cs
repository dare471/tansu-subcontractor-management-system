using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Users.Queries;

public sealed record ListUsersQuery(string? UserType, Guid? SubcontractorId, string? Search)
    : IRequest<IReadOnlyList<UserDto>>;

public sealed class ListUsersHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<ListUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(ListUsersQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanManageTansuUsers, "Управление пользователями доступно только глобальному администратору.");

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

        var users = await q
            .Include(u => u.Subcontractor)
            .Include(u => u.ProjectAssignments).ThenInclude(a => a.Project)
            .Include(u => u.SubcontractorAssignments).ThenInclude(a => a.Subcontractor)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

        return users.Select(UserMapper.ToDto).ToList();
    }
}

internal static class UserMapper
{
    public static UserDto ToDto(Domain.Entities.User u) =>
        new(
            u.Id, u.FullName, u.Position, u.Email, u.UserType,
            u.SubcontractorId,
            u.Subcontractor?.Name,
            u.EmployeeId,
            u.ApproverRole,
            u.TansuRole,
            u.ManagerUserId,
            u.ProjectAssignments.Select(a => a.ProjectOid).ToList(),
            u.ProjectAssignments.Select(a => a.Project?.Name ?? a.ProjectOid.ToString()).ToList(),
            u.SubcontractorAssignments.Select(a => a.SubcontractorId).ToList(),
            u.SubcontractorAssignments.Select(a => a.Subcontractor?.Name ?? a.SubcontractorId.ToString()).ToList(),
            u.MustChangePassword, u.IsActive, u.CreatedAt);
}
