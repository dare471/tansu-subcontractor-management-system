using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
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
    string? ApproverRole) : IRequest<CreateUserResponse>;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.UserType)
            .Must(t => t == Domain.Enums.UserType.Tansu || t == Domain.Enums.UserType.Subcontractor)
            .WithMessage("UserType должен быть TANSU или Subcontractor.");
        RuleFor(x => x.SubcontractorId)
            .NotEmpty().When(x => x.UserType == Domain.Enums.UserType.Subcontractor)
            .WithMessage("Для пользователя-субподрядчика обязателен SubcontractorId.");
    }
}

public sealed class CreateUserHandler(
    ITansuDbContext db,
    IPasswordHasher hasher,
    IPublishEndpoint publisher) : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            throw new ConflictException("email_taken", "Пользователь с таким email уже существует.");

        if (req.SubcontractorId is { } sid &&
            !await db.Subcontractors.AnyAsync(s => s.Id == sid, ct))
        {
            throw new NotFoundException("Subcontractor", sid);
        }

        if (req.UserType == UserType.Tansu && req.ApproverRole is not null &&
            !Domain.Enums.ApproverRole.IsValid(req.ApproverRole))
            throw new ValidationFailedException("Недопустимая роль согласующего.");

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
            PasswordHash = hash,
            MustChangePassword = mustChange,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        await publisher.Publish(new UserCreatedMessage(
            user.Id, user.Email, user.FullName, user.UserType,
            user.SubcontractorId, tempPassword, DateTimeOffset.UtcNow), ct);

        var dto = new UserDto(
            user.Id, user.FullName, user.Position, user.Email, user.UserType,
            user.SubcontractorId, null, user.ApproverRole,
            user.MustChangePassword, user.IsActive, user.CreatedAt);

        return new CreateUserResponse(dto, tempPassword);
    }
}
