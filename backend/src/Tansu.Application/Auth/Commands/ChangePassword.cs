using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Auth.Commands;

public sealed record ChangePasswordCommand(string OldPassword, string NewPassword) : IRequest<Unit>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.OldPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Пароль должен быть не менее 8 символов.")
            .Matches("[A-Z]").WithMessage("Пароль должен содержать заглавную букву.")
            .Matches("[a-z]").WithMessage("Пароль должен содержать строчную букву.")
            .Matches("[0-9]").WithMessage("Пароль должен содержать цифру.");
    }
}

public sealed class ChangePasswordHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPasswordHasher hasher) : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException();

        if (user.UserType != UserType.Subcontractor)
            throw new ForbiddenException("Изменение пароля доступно только субподрядчикам.");

        if (string.IsNullOrEmpty(user.PasswordHash) || !hasher.Verify(request.OldPassword, user.PasswordHash))
            throw new ValidationFailedException("Старый пароль введён неверно.");

        if (hasher.Verify(request.NewPassword, user.PasswordHash))
            throw new ValidationFailedException("Новый пароль должен отличаться от старого.");

        user.PasswordHash = hasher.Hash(request.NewPassword);
        user.MustChangePassword = false;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
