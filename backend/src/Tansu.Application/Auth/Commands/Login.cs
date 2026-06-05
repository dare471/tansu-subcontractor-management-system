using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public sealed class LoginHandler(
    ITansuDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IAppBranding branding) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct)
            ?? throw new UnauthorizedException("Неверный email или пароль.");

        if (!user.IsActive)
            throw new UnauthorizedException("Учётная запись отключена.");

        if (user.UserType != UserType.Subcontractor)
            throw new UnauthorizedException(
                $"Сотрудники {branding.CompanyName} авторизуются через Entra ID.");

        if (string.IsNullOrEmpty(user.PasswordHash) || !hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Неверный email или пароль.");

        var token = jwt.IssueLocalToken(user);

        return new LoginResponse(
            token.AccessToken,
            token.ExpiresAt,
            user.Id,
            user.Email,
            user.UserType,
            user.SubcontractorId,
            user.MustChangePassword);
    }
}
