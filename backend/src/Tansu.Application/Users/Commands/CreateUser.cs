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
    string? EmployerCompany,
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
            .WithMessage("Допустимы учётные записи ТАНСУ или субподрядчика.");
        RuleFor(x => x.SubcontractorId)
            .NotEmpty().When(x => x.UserType == UserType.Subcontractor)
            .WithMessage("Выберите организацию субподрядчика.");
        RuleFor(x => x.EmployerCompany)
            .Must(TansuEmployerCompany.IsValid)
            .When(x => x.UserType == UserType.Tansu)
            .WithMessage("Укажите компанию ТАНСУ.");
        RuleFor(x => x.TansuRole)
            .NotEmpty().When(x => x.UserType == UserType.Tansu)
            .WithMessage("Укажите роль.");
    }
}

public sealed class CreateUserHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IPasswordHasher hasher,
    IPublishEndpoint publisher) : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        var isGlobalFlow = access.Permissions.IsGlobalAdmin || access.Permissions.CanManageTansuUsers;
        UserManagementAccess.EnsureCreate(access, req.UserType, isGlobalFlow);

        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            throw new ConflictException("email_taken", "Пользователь с таким email уже существует.");

        if (req.UserType == UserType.Subcontractor)
        {
            var sid = req.SubcontractorId ?? throw new ValidationFailedException("Выберите организацию.");
            if (!await db.Subcontractors.AnyAsync(s => s.Id == sid, ct))
                throw new NotFoundException("Subcontractor", sid);

            if (!isGlobalFlow)
            {
                var managerId = currentUser.UserId ?? throw new UnauthorizedException();
                await UserManagementAccess.EnsureSubcontractorOwnedByManagerAsync(db, sid, managerId, ct);
            }
        }

        if (req.UserType == UserType.Tansu)
        {
            if (!TansuEmployerCompany.IsValid(req.EmployerCompany))
                throw new ValidationFailedException("Укажите компанию ТАНСУ.");

            if (req.ApproverRole is not null && !ApproverRole.IsValid(req.ApproverRole))
                throw new ValidationFailedException("Недопустимая роль согласующего.");

            if (!TansuRole.IsValid(req.TansuRole))
                throw new ValidationFailedException("Недопустимая роль.");
        }

        if (req.ManagerUserId is { } managerIdCheck &&
            !await db.Users.AnyAsync(x => x.Id == managerIdCheck && x.UserType == UserType.Tansu, ct))
        {
            throw new NotFoundException("User", managerIdCheck);
        }

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
            TansuRole = req.UserType == UserType.Tansu ? req.TansuRole : null,
            EmployerCompany = req.UserType == UserType.Tansu ? req.EmployerCompany : null,
            ManagerUserId = req.UserType == UserType.Tansu ? req.ManagerUserId : null,
            PasswordHash = hash,
            MustChangePassword = mustChange,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

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
