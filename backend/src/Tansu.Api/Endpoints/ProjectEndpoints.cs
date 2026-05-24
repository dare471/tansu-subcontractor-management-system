using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.Projects.Queries;

namespace Tansu.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        g.MapGet("", async (
            [FromQuery] string? search,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListProjectsQuery(search), ct)));

        g.MapPost("", async (
            [FromBody] RegisterProjectCommand cmd,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(cmd, ct)))
        .RequireAuthorization(AuthPolicies.TansuOnly);

        return app;
    }
}
