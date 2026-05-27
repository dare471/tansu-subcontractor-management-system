using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeePortal.Commands;

public sealed record EmployeeLoginCommand(string Iin, string Password) : IRequest<LoginResponse>;

public sealed class EmployeeLoginHandler(
    ITansuDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService jwt) : IRequestHandler<EmployeeLoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(EmployeeLoginCommand request, CancellationToken ct)
    {
        var iin = request.Iin.Trim();
        if (string.IsNullOrEmpty(iin))
            throw new UnauthorizedException("Неверный ИИН или пароль.");

        var employee = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Iin == iin, ct);
        if (employee is null)
            throw new UnauthorizedException("Неверный ИИН или пароль.");

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.EmployeeId == employee.Id && u.UserType == UserType.Employee,
            ct) ?? throw new UnauthorizedException(
            "Личный кабинет ещё не создан. Дождитесь отправки на согласование.");

        if (!user.IsActive)
            throw new UnauthorizedException("Доступ к личному кабинету отключён.");

        if (string.IsNullOrEmpty(user.PasswordHash)
            || !hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Неверный ИИН или пароль.");
        }

        var token = jwt.IssueLocalToken(user);
        return new LoginResponse(
            token.AccessToken,
            token.ExpiresAt,
            user.Id,
            user.Email,
            user.UserType,
            user.SubcontractorId,
            user.MustChangePassword,
            user.EmployeeId);
    }
}
