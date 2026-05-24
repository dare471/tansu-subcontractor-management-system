using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Contracts.Messages;
using Tansu.Domain.Enums;

namespace Tansu.Application.Users.Commands;

public sealed record ResetPasswordCommand(Guid Id) : IRequest<string>;

public sealed class ResetPasswordHandler(
    ITansuDbContext db,
    IPasswordHasher hasher,
    IPublishEndpoint publisher) : IRequestHandler<ResetPasswordCommand, string>
{
    public async Task<string> Handle(ResetPasswordCommand req, CancellationToken ct)
    {
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("User", req.Id);

        if (u.UserType != UserType.Subcontractor)
            throw new ValidationFailedException(
                "Сброс пароля доступен только для пользователей-субподрядчиков.");

        var temp = hasher.GenerateTemporaryPassword();
        u.PasswordHash = hasher.Hash(temp);
        u.MustChangePassword = true;
        await db.SaveChangesAsync(ct);

        await publisher.Publish(new PasswordResetMessage(
            u.Id, u.Email, u.FullName, temp, DateTimeOffset.UtcNow), ct);

        return temp;
    }
}
