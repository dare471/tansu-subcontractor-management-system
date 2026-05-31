using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.Auth;
using Tansu.Application.Auth.Commands;
using Tansu.Application.Auth.Queries;
using Tansu.Application.Projects.Commands;
using Tansu.Application.Subcontractors;

namespace Tansu.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder app,
        IHostEnvironment environment)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
            [FromBody] LoginRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var res = await mediator.Send(new LoginCommand(req.Email, req.Password), ct);
            return Results.Ok(res);
        })
        .AllowAnonymous()
        .WithSummary("Локальный вход по email и паролю (субподрядчики).");

        if (environment.IsDevelopment())
        {
            group.MapPost("/dev-login", async (
                [FromBody] DevLoginRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new DevLoginCommand(req.Email), ct);
                return Results.Ok(res);
            })
            .AllowAnonymous()
            .WithSummary("Локальный вход сотрудника ТАНСУ по email.");
        }

        group.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var res = await mediator.Send(new ChangePasswordCommand(req.OldPassword, req.NewPassword), ct);
            return Results.Ok(res);
        })
        .RequireAuthorization()
        .WithSummary("Смена собственного пароля (субподрядчик или сотрудник).");

        group.MapGet("/me", async (IMediator mediator, CancellationToken ct) =>
        {
            var res = await mediator.Send(new GetMeQuery(), ct);
            return Results.Ok(res);
        })
        .RequireAuthorization()
        .WithSummary("Текущий пользователь.");

        group.MapGet("/me/projects", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetMyProjectsQuery(), ct)))
        .RequireAuthorization(AuthPolicies.SubcontractorOnly)
        .WithSummary("Проекты, привязанные к субподрядчику текущего пользователя.");

        group.MapPut("/me/projects/{projectOid:guid}/progress", async (
            Guid projectOid,
            [FromBody] ReportProjectProgressRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new ReportProjectProgressCommand(projectOid, body.CompletionPercent), ct);
            return Results.NoContent();
        })
        .RequireAuthorization(AuthPolicies.SubcontractorOnly)
        .WithSummary("Отчёт субподрядчика о проценте выполнения работ на проекте.");

        return app;
    }
}
