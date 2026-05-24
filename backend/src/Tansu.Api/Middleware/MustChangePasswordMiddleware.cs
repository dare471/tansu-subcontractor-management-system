using Tansu.Application.Common.Interfaces;

namespace Tansu.Api.Middleware;

/// <summary>
/// Если у субподрядчика установлен флаг must_change_password — разрешаем только
/// эндпоинты смены пароля и /me. Остальные — 403 с кодом must_change_password.
/// </summary>
public sealed class MustChangePasswordMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPaths =
    [
        "/api/auth/change-password",
        "/api/auth/me",
        "/api/auth/login",
        "/health",
        "/swagger",
    ];

    public async Task InvokeAsync(HttpContext ctx, ICurrentUser currentUser)
    {
        if (currentUser.IsAuthenticated && currentUser.MustChangePassword)
        {
            var path = ctx.Request.Path.Value ?? string.Empty;
            var allowed = AllowedPaths.Any(a =>
                path.StartsWith(a, StringComparison.OrdinalIgnoreCase));

            if (!allowed)
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Response.ContentType = "application/problem+json";
                await ctx.Response.WriteAsJsonAsync(new
                {
                    type = "about:blank",
                    title = "Password change required",
                    status = 403,
                    code = "must_change_password",
                    detail = "Перед использованием системы необходимо сменить временный пароль."
                });
                return;
            }
        }

        await next(ctx);
    }
}
