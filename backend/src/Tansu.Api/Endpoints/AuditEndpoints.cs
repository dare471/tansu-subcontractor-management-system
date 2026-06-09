using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Application.Audit.Queries;

namespace Tansu.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit-events", async (
            [FromQuery] string? action,
            [FromQuery] string? entityType,
            [FromQuery] Guid? entityId,
            [FromQuery] Guid? actorUserId,
            [FromQuery] Guid? projectOid,
            [FromQuery] Guid? subcontractorId,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListAuditEventsQuery(
                page ?? 1, pageSize ?? 50, action, entityType, entityId, actorUserId,
                projectOid, subcontractorId, from, to), ct);
            return Results.Ok(result);
        })
        .WithTags("Audit")
        .RequireAuthorization();

        return app;
    }
}
