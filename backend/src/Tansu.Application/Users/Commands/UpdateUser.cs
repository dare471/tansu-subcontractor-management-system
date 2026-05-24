using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Users.Commands;

public sealed record UpdateUserCommand(
    Guid Id, string FullName, string Position, bool IsActive, string? ApproverRole) : IRequest<UserDto>;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(300);
    }
}

public sealed class UpdateUserHandler(ITansuDbContext db)
    : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var u = await db.Users.Include(x => x.Subcontractor)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("User", req.Id);

        u.FullName = req.FullName.Trim();
        u.Position = req.Position.Trim();
        u.IsActive = req.IsActive;

        if (u.UserType == UserType.Tansu)
        {
            if (req.ApproverRole is not null && !Domain.Enums.ApproverRole.IsValid(req.ApproverRole))
                throw new ValidationFailedException("Недопустимая роль согласующего.");
            u.ApproverRole = string.IsNullOrWhiteSpace(req.ApproverRole) ? null : req.ApproverRole;
        }

        await db.SaveChangesAsync(ct);

        return new UserDto(
            u.Id, u.FullName, u.Position, u.Email, u.UserType,
            u.SubcontractorId, u.Subcontractor?.Name,
            u.ApproverRole, u.MustChangePassword, u.IsActive, u.CreatedAt);
    }
}
