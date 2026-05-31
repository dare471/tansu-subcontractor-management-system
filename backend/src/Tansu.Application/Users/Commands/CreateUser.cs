using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Users.Queries;
using Tansu.Contracts.Messages;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Users.Commands;

public sealed record CreateUserCommand(
    string FullName,
    string Position,
    string Email,
    string UserType,
    Guid? SubcontractorId,
    string? ApproverRole,
    string? TansuRole,
    Guid? ManagerUserId,
    IReadOnlyList<Guid>? ProjectOids,
    IReadOnlyList<Guid>? SubcontractorIds) : IRequest<CreateUserResponse>;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.UserType)
            .Must(t => t == UserType.Tansu || t == UserType.Subcontractor)
            .WithMessage("UserType должен быть TANSU или Subcontractor.");
        RuleFor(x => x.SubcontractorId)
            .NotEmpty().When(x => x.UserType == UserType.Subcontractor)
            .WithMessage("Для пользователя-субподрядчика обязателен SubcontractorId.");
    }
}

public sealed class CreateUserHandler(
    ITansuDbContext db,
    ITansuAccessService accessService,
    IPasswordHasher hasher,
    IPublishEndpoint publisher) : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanManageTansuUsers, "Управление пользователями доступно только глобальному администратору.");

        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            throw new ConflictException("email_taken", "Пользователь с таким email уже существует.");

        if (req.SubcontractorId is { } sid &&
            !await db.Subcontractors.AnyAsync(s => s.Id == sid, ct))
            throw new NotFoundException("Subcontractor", sid);

        if (req.UserType == UserType.Tansu && req.ApproverRole is not null &&
            !ApproverRole.IsValid(req.ApproverRole))
            throw new ValidationFailedException("Недопустимая роль согласующего.");

        if (req.UserType == UserType.Tansu && req.TansuRole is not null &&
            !TansuRole.IsValid(req.TansuRole))
            throw new ValidationFailedException("Недопустимая роль ТАНСУ.");

        if (req.ManagerUserId is { } managerId &&
            !await db.Users.AnyAsync(x => x.Id == managerId && x.UserType == UserType.Tansu, ct))
            throw new NotFoundException("User", managerId);

        string? tempPassword = null;
        string? hash = null;
        var mustChange = false;

        if (req.UserType == UserType.Subcontractor)
        {
            tempPassword = hasher.GenerateTemporaryPassword();
            hash = hasher.Hash(tempPassword);
            mustChange = true;
        }

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Position = req.Position.Trim(),
            Email = email,
            UserType = req.UserType,
            SubcontractorId = req.SubcontractorId,
            ApproverRole = req.UserType == UserType.Tansu && !string.IsNullOrWhiteSpace(req.ApproverRole)
                ? req.ApproverRole
                : null,
            TansuRole = req.UserType == UserType.Tansu && !string.IsNullOrWhiteSpace(req.TansuRole)
                ? req.TansuRole
                : null,
            ManagerUserId = req.UserType == UserType.Tansu ? req.ManagerUserId : null,
            PasswordHash = hash,
            MustChangePassword = mustChange,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        if (req.UserType == UserType.Tansu)
        {
            if (req.ProjectOids is { Count: > 0 } projectOids)
                await UserAssignmentSync.SyncProjectsAsync(db, user, projectOids, ct);
            if (req.SubcontractorIds is { Count: > 0 } subIds)
                await UserAssignmentSync.SyncSubcontractorsAsync(db, user, subIds, ct);
            await db.SaveChangesAsync(ct);
        }

        await publisher.Publish(new UserCreatedMessage(
            user.Id, user.Email, user.FullName, user.UserType,
            user.SubcontractorId, tempPassword, DateTimeOffset.UtcNow), ct);

        user = await db.Users.AsNoTracking()
            .Include(u => u.Subcontractor)
            .Include(u => u.ProjectAssignments).ThenInclude(a => a.Project)
            .Include(u => u.SubcontractorAssignments).ThenInclude(a => a.Subcontractor)
            .FirstAsync(u => u.Id == user.Id, ct);

        return new CreateUserResponse(UserMapper.ToDto(user), tempPassword);
    }
}
