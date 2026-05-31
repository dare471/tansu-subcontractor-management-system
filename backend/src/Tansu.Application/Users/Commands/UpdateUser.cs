using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Users.Queries;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Users.Commands;

public sealed record UpdateUserCommand(
    Guid Id,
    string FullName,
    string Position,
    bool IsActive,
    string? StatusComment,
    string? ApproverRole,
    string? TansuRole,
    Guid? ManagerUserId,
    IReadOnlyList<Guid>? ProjectOids,
    IReadOnlyList<Guid>? SubcontractorIds) : IRequest<UserDto>;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(300);
    }
}

public sealed class UpdateUserHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanManageTansuUsers, "Управление пользователями доступно только глобальному администратору.");

        var u = await db.Users
            .Include(x => x.Subcontractor)
            .Include(x => x.ProjectAssignments).ThenInclude(a => a.Project)
            .Include(x => x.SubcontractorAssignments).ThenInclude(a => a.Subcontractor)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("User", req.Id);

        var wasActive = u.IsActive;

        u.FullName = req.FullName.Trim();
        u.Position = req.Position.Trim();
        u.IsActive = req.IsActive;

        if (wasActive != req.IsActive)
        {
            if (!req.IsActive)
            {
                if (string.IsNullOrWhiteSpace(req.StatusComment) || req.StatusComment.Trim().Length < 3)
                    throw new ValidationFailedException("Укажите причину блокировки (не короче 3 символов).");
            }

            var reason = req.IsActive
                ? (string.IsNullOrWhiteSpace(req.StatusComment) ? "Разблокировка" : req.StatusComment.Trim())
                : req.StatusComment!.Trim();

            var initiatorId = currentUser.UserId ?? throw new UnauthorizedException();

            db.UserBlockRecords.Add(new UserBlockRecord
            {
                UserId = u.Id,
                InitiatedByUserId = initiatorId,
                ActionType = req.IsActive ? EmployeeBlockActionType.Unblock : EmployeeBlockActionType.Block,
                Reason = reason
            });
        }

        if (u.UserType == UserType.Tansu)
        {
            if (req.ApproverRole is not null && !ApproverRole.IsValid(req.ApproverRole))
                throw new ValidationFailedException("Недопустимая роль согласующего.");
            u.ApproverRole = string.IsNullOrWhiteSpace(req.ApproverRole) ? null : req.ApproverRole;

            if (req.TansuRole is not null && !TansuRole.IsValid(req.TansuRole))
                throw new ValidationFailedException("Недопустимая роль ТАНСУ.");
            u.TansuRole = string.IsNullOrWhiteSpace(req.TansuRole) ? null : req.TansuRole;

            if (req.ManagerUserId is { } managerId)
            {
                if (managerId == u.Id)
                    throw new ValidationFailedException("Пользователь не может быть своим руководителем.");
                if (!await db.Users.AnyAsync(x => x.Id == managerId && x.UserType == UserType.Tansu, ct))
                    throw new NotFoundException("User", managerId);
            }
            u.ManagerUserId = req.ManagerUserId;

            if (req.ProjectOids is not null)
                await UserAssignmentSync.SyncProjectsAsync(db, u, req.ProjectOids, ct);
            if (req.SubcontractorIds is not null)
                await UserAssignmentSync.SyncSubcontractorsAsync(db, u, req.SubcontractorIds, ct);
        }

        await db.SaveChangesAsync(ct);

        var blockReason = await UserBlockReasonLookup.GetAsync(db, u.Id, u.IsActive, ct);
        return UserMapper.ToDto(u, blockReason);
    }
}

internal static class UserBlockReasonLookup
{
    public static async Task<string?> GetAsync(
        ITansuDbContext db, Guid userId, bool isActive, CancellationToken ct)
    {
        if (isActive) return null;

        var last = await db.UserBlockRecords.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.ActionType, r.Reason })
            .FirstOrDefaultAsync(ct);

        return last?.ActionType == EmployeeBlockActionType.Block ? last.Reason : null;
    }

    public static async Task<IReadOnlyDictionary<Guid, string?>> GetManyAsync(
        ITansuDbContext db, IReadOnlyList<Domain.Entities.User> users, CancellationToken ct)
    {
        var inactiveIds = users.Where(u => !u.IsActive).Select(u => u.Id).ToList();
        if (inactiveIds.Count == 0)
            return new Dictionary<Guid, string?>();

        var rows = await db.UserBlockRecords.AsNoTracking()
            .Where(r => inactiveIds.Contains(r.UserId))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.UserId, r.ActionType, r.Reason })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.First().ActionType == EmployeeBlockActionType.Block ? g.First().Reason : null);
    }
}

internal static class UserAssignmentSync
{
    public static async Task SyncProjectsAsync(
        ITansuDbContext db,
        Domain.Entities.User user,
        IReadOnlyList<Guid> projectOids,
        CancellationToken ct)
    {
        var desired = projectOids.Distinct().ToHashSet();
        var existing = user.ProjectAssignments.ToList();

        foreach (var row in existing.Where(a => !desired.Contains(a.ProjectOid)))
            db.UserProjectAssignments.Remove(row);

        var current = existing.Select(a => a.ProjectOid).ToHashSet();
        foreach (var oid in desired.Where(id => !current.Contains(id)))
        {
            if (!await db.ProjectRefs.AnyAsync(p => p.ProjectOid == oid, ct))
                throw new NotFoundException("Project", oid);

            db.UserProjectAssignments.Add(new UserProjectAssignment
            {
                UserId = user.Id,
                ProjectOid = oid
            });
        }
    }

    public static async Task SyncSubcontractorsAsync(
        ITansuDbContext db,
        Domain.Entities.User user,
        IReadOnlyList<Guid> subcontractorIds,
        CancellationToken ct)
    {
        var desired = subcontractorIds.Distinct().ToHashSet();
        var existing = user.SubcontractorAssignments.ToList();

        foreach (var row in existing.Where(a => !desired.Contains(a.SubcontractorId)))
            db.UserSubcontractorAssignments.Remove(row);

        var current = existing.Select(a => a.SubcontractorId).ToHashSet();
        foreach (var sid in desired.Where(id => !current.Contains(id)))
        {
            if (!await db.Subcontractors.AnyAsync(s => s.Id == sid, ct))
                throw new NotFoundException("Subcontractor", sid);

            db.UserSubcontractorAssignments.Add(new UserSubcontractorAssignment
            {
                UserId = user.Id,
                SubcontractorId = sid
            });
        }
    }
}
