using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Api.Auth;

/// <summary>
/// При первом успешном валидном JWT от Entra ID создаёт/обновляет пользователя
/// типа TANSU в локальной БД и добавляет claim "sub"=<user_id> в Principal.
/// </summary>
public sealed class EntraUserProvisioner(TansuDbContext db, ILogger<EntraUserProvisioner> logger)
{
    public async Task ProvisionAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal is null) return;

        var email = principal.FindFirstValue("preferred_username")
                    ?? principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("Entra token has no email/preferred_username; cannot provision.");
            return;
        }

        email = email.Trim().ToLowerInvariant();
        var name = principal.FindFirstValue("name") ?? email;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user is null)
        {
            user = new User
            {
                Email = email,
                FullName = name,
                Position = "—",
                UserType = UserType.Tansu,
                PasswordHash = null,
                IsActive = true,
                MustChangePassword = false
            };
            db.Users.Add(user);
            logger.LogInformation("Auto-provisioned TANSU user {Email} (id={Id}).", email, user.Id);
        }
        else if (user.UserType != UserType.Tansu)
        {
            logger.LogWarning("Email {Email} exists in DB with user_type={Type}; refusing Entra auth.",
                email, user.UserType);
            context.Fail("Account type mismatch.");
            return;
        }

        await db.SaveChangesAsync();

        if (principal.Identity is ClaimsIdentity ci)
        {
            ci.AddClaim(new Claim("sub", user.Id.ToString()));
            ci.AddClaim(new Claim("user_type", UserType.Tansu));
            ci.AddClaim(new Claim("email", user.Email));
        }
    }
}
