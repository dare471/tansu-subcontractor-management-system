using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.Users;
using Tansu.Application.Users.Commands;
using Tansu.Application.Users.Queries;

namespace Tansu.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapGet("", async (
            [FromQuery] string? userType,
            [FromQuery] Guid? subcontractorId,
            [FromQuery] string? search,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListUsersQuery(userType, subcontractorId, search), ct)))
            .WithSummary("Список пользователей (только глобальный администратор).");

        g.MapPost("", async (
            [FromBody] CreateUserRequest req,
            IMediator m, CancellationToken ct) =>
        {
            var res = await m.Send(new CreateUserCommand(
                req.FullName, req.Position, req.Email, req.UserType, req.SubcontractorId,
                req.ApproverRole, req.TansuRole, req.ManagerUserId,
                req.ProjectOids, req.SubcontractorIds), ct);
            return Results.Created($"/api/users/{res.User.Id}", res);
        });

        g.MapPut("/{id:guid}", async (
            Guid id, [FromBody] UpdateUserRequest req,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new UpdateUserCommand(
                    id, req.FullName, req.Position, req.IsActive, req.StatusComment, req.ApproverRole,
                    req.TansuRole, req.ManagerUserId, req.ProjectOids, req.SubcontractorIds), ct)));

        g.MapGet("/{id:guid}/blocks", async (
            Guid id, IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new GetUserBlockStatusQuery(id), ct)));

        g.MapPost("/{id:guid}/reset-password", async (
            Guid id, IMediator m, CancellationToken ct) =>
        {
            var tempPassword = await m.Send(new ResetPasswordCommand(id), ct);
            return Results.Ok(new { temporaryPassword = tempPassword });
        });

        return app;
    }
}
