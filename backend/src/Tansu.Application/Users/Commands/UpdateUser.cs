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
    string? EmployerCompany,
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

        var u = await db.Users
            .Include(x => x.Subcontractor)
            .Include(x => x.ProjectAssignments).ThenInclude(a => a.Project)
            .Include(x => x.SubcontractorAssignments).ThenInclude(a => a.Subcontractor)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("User", req.Id);

        UserManagementAccess.EnsureManageUser(access, u, currentUser.UserId);

        if (u.UserType == UserType.Subcontractor && access.Permissions.CanManageSubcontractorUsers &&
            !access.Permissions.CanManageTansuUsers && !access.Permissions.IsGlobalAdmin)
        {
            var managerId = currentUser.UserId ?? throw new UnauthorizedException();
            if (u.SubcontractorId is { } sid)
                await UserManagementAccess.EnsureSubcontractorOwnedByManagerAsync(db, sid, managerId, ct);
        }

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

        if (u.UserType == UserType.Tansu &&
            (access.Permissions.CanManageTansuUsers || access.Permissions.IsGlobalAdmin))
        {
            if (req.ApproverRole is not null && !ApproverRole.IsValid(req.ApproverRole))
                throw new ValidationFailedException("Недопустимая роль согласующего.");
            u.ApproverRole = string.IsNullOrWhiteSpace(req.ApproverRole) ? null : req.ApproverRole;

            if (req.TansuRole is not null && !TansuRole.IsValid(req.TansuRole))
                throw new ValidationFailedException("Недопустимая роль ТАНСУ.");
            if (!string.IsNullOrWhiteSpace(req.TansuRole))
                u.TansuRole = req.TansuRole;

            if (req.EmployerCompany is not null)
            {
                if (!TansuEmployerCompany.IsValid(req.EmployerCompany))
                    throw new ValidationFailedException("Недопустимая компания.");
                u.EmployerCompany = req.EmployerCompany;
            }

            if (req.ManagerUserId is { } managerId)
            {
                if (managerId == u.Id)
                    throw new ValidationFailedException("Пользователь не может быть своим руководителем.");
                if (!await db.Users.AnyAsync(x => x.Id == managerId && x.UserType == UserType.Tansu, ct))
                    throw new NotFoundException("User", managerId);
                u.ManagerUserId = req.ManagerUserId;
            }
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
