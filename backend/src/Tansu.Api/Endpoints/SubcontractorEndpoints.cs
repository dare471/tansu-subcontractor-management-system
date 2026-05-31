using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.Subcontractors;
using Tansu.Application.Subcontractors.Commands;
using Tansu.Application.Subcontractors.Queries;

namespace Tansu.Api.Endpoints;

public static class SubcontractorEndpoints
{
    public static IEndpointRouteBuilder MapSubcontractorEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/subcontractors")
            .WithTags("Subcontractors")
            .RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapGet("", async (
            [FromQuery] string? search,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListSubcontractorsQuery(search), ct)));

        g.MapPost("", async (
            [FromBody] CreateSubcontractorRequest req,
            IMediator m, CancellationToken ct) =>
        {
            var dto = await m.Send(new CreateSubcontractorCommand(req.Name, req.Bin), ct);
            return Results.Created($"/api/subcontractors/{dto.Id}", dto);
        });

        g.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateSubcontractorRequest req,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new UpdateSubcontractorCommand(id, req.Name, req.Bin), ct)));

        g.MapDelete("/{id:guid}", async (
            Guid id, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new DeleteSubcontractorCommand(id), ct);
            return Results.NoContent();
        });

        g.MapGet("/{id:guid}/projects", async (
            Guid id, IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListProjectsForSubcontractorQuery(id), ct)));

        g.MapPost("/{id:guid}/projects", async (
            Guid id, [FromBody] BindProjectRequest req,
            IMediator m, CancellationToken ct) =>
        {
            await m.Send(new BindProjectCommand(id, req.ProjectOid, req.ProjectName, req.ActivityType), ct);
            return Results.NoContent();
        });

        g.MapDelete("/{id:guid}/projects/{projectOid:guid}", async (
            Guid id, Guid projectOid, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new UnbindProjectCommand(id, projectOid), ct);
            return Results.NoContent();
        });

        return app;
    }
}
