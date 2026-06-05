using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Users.Commands;
using Tansu.Domain.Enums;

namespace Tansu.Application.Users.Queries;

public sealed record ListUsersQuery(string? UserType, Guid? SubcontractorId, string? Search)
    : IRequest<IReadOnlyList<UserDto>>;

public sealed class ListUsersHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<ListUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(ListUsersQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        UserManagementAccess.EnsureList(access, req.UserType);

        var q = db.Users.AsNoTracking().AsQueryable();

        var scopedToManager = !access.Permissions.IsGlobalAdmin &&
                              !access.Permissions.CanManageTansuUsers &&
                              access.Permissions.CanManageSubcontractorUsers;

        if (scopedToManager)
        {
            var managerId = currentUser.UserId ?? throw new UnauthorizedException();
            q = q.Where(u =>
                u.UserType == UserType.Subcontractor &&
                u.SubcontractorId != null &&
                db.Subcontractors.Any(s =>
                    s.Id == u.SubcontractorId &&
                    (s.RegisteredByUserId == managerId || s.ManagerUserId == managerId)));
            req = req with { UserType = UserType.Subcontractor };
        }
        else if (!access.Permissions.IsGlobalAdmin && !access.Permissions.CanManageTansuUsers)
        {
            q = q.Where(u => u.UserType == UserType.Tansu);
        }

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

        var blockReasons = await UserBlockReasonLookup.GetManyAsync(db, users, ct);

        return users
            .Select(u => UserMapper.ToDto(u, blockReasons.GetValueOrDefault(u.Id)))
            .ToList();
    }
}

internal static class UserMapper
{
    public static UserDto ToDto(Domain.Entities.User u, string? blockReason = null) =>
        new(
            u.Id, u.FullName, u.Position, u.Email, u.UserType,
            u.SubcontractorId,
            u.Subcontractor?.Name,
            u.EmployeeId,
            u.ApproverRole,
            u.TansuRole,
            u.EmployerCompany,
            u.ManagerUserId,
            u.ProjectAssignments.Select(a => a.ProjectOid).ToList(),
            u.ProjectAssignments.Select(a => a.Project?.Name ?? a.ProjectOid.ToString()).ToList(),
            u.SubcontractorAssignments.Select(a => a.SubcontractorId).ToList(),
            u.SubcontractorAssignments.Select(a => a.Subcontractor?.Name ?? a.SubcontractorId.ToString()).ToList(),
            u.MustChangePassword, u.IsActive, blockReason, u.CreatedAt);
}
