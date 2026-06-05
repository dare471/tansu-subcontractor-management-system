using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Auth.Commands;

public sealed record DevLoginCommand(string Email) : IRequest<LoginResponse>;

public sealed class DevLoginValidator : AbstractValidator<DevLoginCommand>
{
    public DevLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class DevLoginHandler(
    ITansuDbContext db,
    IJwtTokenService jwt,
    IHostEnvironment env,
    IAppBranding branding) : IRequestHandler<DevLoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(DevLoginCommand request, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            throw new ForbiddenException("Локальный вход доступен только в среде разработки.");

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct)
            ?? throw new UnauthorizedException("Пользователь не найден.");

        if (user.UserType != UserType.Tansu)
            throw new ValidationFailedException(
                $"Локальный вход только для сотрудников {branding.CompanyName}.");

        if (!user.IsActive)
            throw new UnauthorizedException("Учётная запись отключена.");

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
