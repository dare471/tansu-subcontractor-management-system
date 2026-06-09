using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Application.Incidents;

namespace Tansu.Api.Endpoints;

public static class IncidentEndpoints
{
    public static IEndpointRouteBuilder MapIncidentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/incidents", async (
            [FromQuery] Guid? projectOid,
            [FromQuery] Guid? subcontractorId,
            [FromQuery] string? status,
            IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListSiteIncidentsQuery(projectOid, subcontractorId, status), ct)))
            .RequireAuthorization();

        app.MapPost("/api/incidents", async (CreateIncidentBody body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateSiteIncidentCommand(
                body.ProjectOid, body.OccurredAt, body.Title, body.Description, body.Severity,
                body.SubcontractorId, body.BlockUntilResolved, body.EmployeeIds), ct);
            return Results.Created($"/api/incidents/{result.Id}", result);
        }).RequireAuthorization();

        app.MapPatch("/api/incidents/{id:guid}", async (
            Guid id, UpdateIncidentBody body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateSiteIncidentStatusCommand(id, body.Status, body.ResolutionNotes), ct);
            return Results.Ok(result);
        }).RequireAuthorization();

        return app;
    }

    private sealed record CreateIncidentBody(
        Guid ProjectOid,
        DateTimeOffset OccurredAt,
        string Title,
        string Description,
        string Severity,
        Guid? SubcontractorId,
        bool BlockUntilResolved,
        IReadOnlyList<Guid> EmployeeIds);

    private sealed record UpdateIncidentBody(string Status, string? ResolutionNotes);
}
