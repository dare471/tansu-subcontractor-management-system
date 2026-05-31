using Microsoft.EntityFrameworkCore;
using Tansu.Application.Approvals;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Domain.Enums;

namespace Tansu.Application.Auth;

public sealed class TansuAccessService(ITansuDbContext db, ICurrentUser currentUser) : ITansuAccessService
{
    public async Task<TansuAccessContext> GetAccessAsync(CancellationToken ct)
    {
        if (currentUser.UserType == UserType.Subcontractor)
        {
            return new TansuAccessContext(
                null,
                SubcontractorPermissions(),
                currentUser.SubcontractorId is { } sid ? new HashSet<Guid> { sid } : new HashSet<Guid>(),
                null,
                false);
        }

        if (currentUser.UserType != UserType.Tansu)
        {
            return new TansuAccessContext(
                null,
                DenyAll(),
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                false);
        }

        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var user = await db.Users.AsNoTracking()
            .FirstAsync(u => u.Id == userId, ct);

        var role = ResolveEffectiveRole(user);
        var assignedProjects = await db.UserProjectAssignments.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.ProjectOid)
            .ToListAsync(ct);
        var assignedSubcontractors = await db.UserSubcontractorAssignments.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.SubcontractorId)
            .ToListAsync(ct);

        var permissions = BuildPermissions(role, user.IsSuperUser);
        var (subIds, projectOids, includeInactive) = await ResolveScopeAsync(
            role, userId, assignedProjects, permissions, ct);

        (subIds, projectOids) = ApplyExplicitVisibility(
            subIds,
            projectOids,
            assignedSubcontractors,
            assignedProjects,
            role);

        return new TansuAccessContext(role, permissions, subIds, projectOids, includeInactive);
    }

    public async Task EnsureSubcontractorVisibleAsync(Guid subcontractorId, CancellationToken ct)
    {
        var access = await GetAccessAsync(ct);
        if (access.Permissions.IsGlobalAdmin || access.VisibleSubcontractorIds is null)
            return;
        if (!access.VisibleSubcontractorIds.Contains(subcontractorId))
            throw new ForbiddenException("Субподрядчик вне вашей области видимости.");
    }

    public async Task EnsureEmployeeVisibleAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw new NotFoundException("Employee", employeeId);

        await EnsureSubcontractorVisibleAsync(employee.SubcontractorId, ct);

        var access = await GetAccessAsync(ct);
        if (access.VisibleProjectOids is null)
            return;
        if (!access.VisibleProjectOids.Contains(employee.ProjectOid))
            throw new ForbiddenException("Сотрудник вне вашей области видимости.");
    }

    public void EnsurePermission(
        TansuAccessContext access,
        Func<TansuPermissionsDto, bool> check,
        string message)
    {
        if (!check(access.Permissions))
            throw new ForbiddenException(message);
    }

    private static string? ResolveEffectiveRole(Domain.Entities.User user)
    {
        if (user.IsSuperUser)
            return TansuRole.GlobalAdmin;
        if (!string.IsNullOrWhiteSpace(user.TansuRole) && TansuRole.IsValid(user.TansuRole))
            return user.TansuRole;

        return user.ApproverRole switch
        {
            ApproverRole.OID => TansuRole.OidManager,
            ApproverRole.Safety => TansuRole.SafetyProject,
            ApproverRole.Security => TansuRole.SbProject,
            ApproverRole.Management => TansuRole.OidDirector,
            _ => null
        };
    }

    private static TansuPermissionsDto BuildPermissions(string? role, bool isSuperUser)
    {
        if (isSuperUser || role == TansuRole.GlobalAdmin)
        {
            return new TansuPermissionsDto(
                true, true, true, true, true, true, true, true, true, false, true);
        }

        return role switch
        {
            TansuRole.OidManager => new(
                CanRegisterSubcontractors: true,
                CanManageApprovalMatrix: true,
                CanApproveEmployees: true,
                CanBlockEmployees: true,
                CanViewVisitJournal: false,
                CanManageTansuUsers: false,
                CanManageSubordinates: false,
                CanViewEmployees: true,
                CanUploadDocuments: true,
                IsReadOnlyMonitoring: false,
                IsGlobalAdmin: false),
            TansuRole.OidDirector => new(
                false, false, true, true, false, false, false, true, true, false, false),
            TansuRole.SbProject => new(
                false, false, false, true, false, false, false, true, false, false, false),
            TansuRole.SbChief => new(
                false, false, false, true, true, false, false, true, false, false, false),
            TansuRole.SafetyProject => new(
                false, false, false, true, false, false, false, true, false, false, false),
            TansuRole.SafetyChief => new(
                false, false, false, true, true, false, false, true, false, false, false),
            TansuRole.ProjectManager => new(
                false, false, false, true, false, false, false, true, false, true, false),
            _ => DenyAll()
        };
    }

    private async Task<(IReadOnlySet<Guid>? SubIds, IReadOnlySet<Guid>? ProjectOids, bool IncludeInactive)>
        ResolveScopeAsync(
            string? role,
            Guid userId,
            IReadOnlyList<Guid> assignedProjects,
            TansuPermissionsDto permissions,
            CancellationToken ct)
    {
        if (permissions.IsGlobalAdmin)
            return (null, null, true);

        return role switch
        {
            TansuRole.OidManager => (
                await SubcontractorsRegisteredByAsync(new HashSet<Guid> { userId }, includeInactive: true, ct),
                null,
                true),
            TansuRole.OidDirector => (
                await SubcontractorsRegisteredByAsync(await DescendantUserIdsAsync(userId, ct), includeInactive: true, ct),
                null,
                true),
            TansuRole.SbChief or TansuRole.SafetyChief => (
                await ActiveSubcontractorIdsAsync(ct),
                null,
                false),
            TansuRole.SbProject or TansuRole.SafetyProject => (
                await ActivelyWorkingSubcontractorsOnProjectsAsync(assignedProjects, ct),
                assignedProjects.Count == 0 ? new HashSet<Guid>() : assignedProjects.ToHashSet(),
                false),
            TansuRole.ProjectManager => (
                await ActiveSubcontractorsOnProjectsAsync(assignedProjects, ct),
                assignedProjects.Count == 0 ? new HashSet<Guid>() : assignedProjects.ToHashSet(),
                false),
            _ => (new HashSet<Guid>(), new HashSet<Guid>(), false)
        };
    }

    private async Task<IReadOnlySet<Guid>> DescendantUserIdsAsync(Guid rootUserId, CancellationToken ct)
    {
        var allUsers = await db.Users.AsNoTracking()
            .Where(u => u.UserType == UserType.Tansu && u.ManagerUserId != null)
            .Select(u => new { u.Id, u.ManagerUserId })
            .ToListAsync(ct);

        var result = new HashSet<Guid> { rootUserId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootUserId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in allUsers.Where(u => u.ManagerUserId == current))
            {
                if (result.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    private async Task<IReadOnlySet<Guid>> SubcontractorsRegisteredByAsync(
        IReadOnlySet<Guid> userIds,
        bool includeInactive,
        CancellationToken ct)
    {
        var q = db.Subcontractors.AsNoTracking()
            .Where(s => s.RegisteredByUserId != null && userIds.Contains(s.RegisteredByUserId.Value));
        if (!includeInactive)
            q = q.Where(s => s.IsActive);
        return (await q.Select(s => s.Id).ToListAsync(ct)).ToHashSet();
    }

    private async Task<IReadOnlySet<Guid>> ActiveSubcontractorIdsAsync(CancellationToken ct) =>
        (await db.Subcontractors.AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(ct)).ToHashSet();

    private async Task<IReadOnlySet<Guid>> ActiveSubcontractorsOnProjectsAsync(
        IReadOnlyList<Guid> projectOids,
        CancellationToken ct)
    {
        if (projectOids.Count == 0)
            return new HashSet<Guid>();

        return (await db.ProjectSubcontractors.AsNoTracking()
            .Where(ps => projectOids.Contains(ps.ProjectOid))
            .Join(db.Subcontractors.Where(s => s.IsActive), ps => ps.SubcontractorId, s => s.Id, (_, s) => s.Id)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();
    }

    private async Task<IReadOnlySet<Guid>> ActivelyWorkingSubcontractorsOnProjectsAsync(
        IReadOnlyList<Guid> projectOids,
        CancellationToken ct)
    {
        if (projectOids.Count == 0)
            return new HashSet<Guid>();

        var activeOnProjects = await ActiveSubcontractorsOnProjectsAsync(projectOids, ct);
        if (activeOnProjects.Count == 0)
            return activeOnProjects;

        var employees = await db.Employees.AsNoTracking()
            .Where(e => activeOnProjects.Contains(e.SubcontractorId) && projectOids.Contains(e.ProjectOid))
            .Select(e => new { e.Id, e.SubcontractorId })
            .ToListAsync(ct);

        if (employees.Count == 0)
            return new HashSet<Guid>();

        var employeeIds = employees.Select(e => e.Id).ToList();
        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId))
            .ToListAsync(ct);

        var sheetsByEmployee = sheets.GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Domain.Entities.ApprovalSheetEntry>)g.ToList());

        var workingSubs = new HashSet<Guid>();
        foreach (var employee in employees)
        {
            sheetsByEmployee.TryGetValue(employee.Id, out var employeeSheets);
            employeeSheets ??= Array.Empty<Domain.Entities.ApprovalSheetEntry>();
            if (EmployeeStatusResolver.ResolveFromSheets(employeeSheets) == ApprovalStatus.Approved)
                workingSubs.Add(employee.SubcontractorId);
        }

        return workingSubs;
    }

    private static (IReadOnlySet<Guid>? SubIds, IReadOnlySet<Guid>? ProjectOids) ApplyExplicitVisibility(
        IReadOnlySet<Guid>? roleSubs,
        IReadOnlySet<Guid>? roleProjects,
        IReadOnlyList<Guid> explicitSubIds,
        IReadOnlyList<Guid> explicitProjectOids,
        string? role)
    {
        var explicitSubs = explicitSubIds.Count == 0 ? null : explicitSubIds.ToHashSet();
        var explicitProjects = explicitProjectOids.Count == 0 ? null : explicitProjectOids.ToHashSet();

        if (explicitSubs is null && explicitProjects is null)
        {
            if (RoleHasInherentSubScope(role))
                return (roleSubs, roleProjects);
            if (role is TansuRole.SbProject or TansuRole.SafetyProject or TansuRole.ProjectManager)
                return (roleSubs, roleProjects);
            if (roleSubs is null && roleProjects is null)
                return (null, null);
            return (new HashSet<Guid>(), new HashSet<Guid>());
        }

        var finalSubs = roleSubs;
        var finalProjects = roleProjects;

        if (explicitSubs is not null)
        {
            finalSubs = roleSubs is null
                ? explicitSubs
                : IntersectSets(roleSubs, explicitSubs);
        }

        if (explicitProjects is not null)
        {
            finalProjects = roleProjects is null
                ? explicitProjects
                : roleProjects.Count == 0
                    ? explicitProjects
                    : IntersectSets(roleProjects, explicitProjects);
        }

        return (finalSubs ?? new HashSet<Guid>(), finalProjects ?? new HashSet<Guid>());
    }

    private static bool RoleHasInherentSubScope(string? role) =>
        role is TansuRole.OidManager or TansuRole.OidDirector
            or TansuRole.SbChief or TansuRole.SafetyChief;

    private static HashSet<Guid> IntersectSets(IReadOnlySet<Guid> left, IReadOnlySet<Guid> right)
    {
        var smaller = left.Count <= right.Count ? left : right;
        var other = ReferenceEquals(smaller, left) ? right : left;
        return smaller.Where(other.Contains).ToHashSet();
    }

    private static TansuPermissionsDto SubcontractorPermissions() =>
        new(false, false, false, false, false, false, false, true, true, false, false);

    private static TansuPermissionsDto DenyAll() =>
        new(false, false, false, false, false, false, false, false, false, false, false);
}
