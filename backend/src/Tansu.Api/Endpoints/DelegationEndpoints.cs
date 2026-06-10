using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Application.Delegations;

namespace Tansu.Api.Endpoints;

public static class DelegationEndpoints
{
    public static IEndpointRouteBuilder MapDelegationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/delegations", async (
            [FromQuery] bool activeOnly,
            IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListApproverDelegationsQuery(activeOnly), ct)))
            .RequireAuthorization();

        app.MapPost("/api/delegations", async (CreateDelegationBody body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateApproverDelegationCommand(
                body.DelegateUserId, body.ProjectOid, body.SubcontractorId,
                body.ApproverRole, body.ValidFrom, body.ValidTo), ct);
            return Results.Ok(result);
        }).RequireAuthorization();

        app.MapDelete("/api/delegations/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new RevokeApproverDelegationCommand(id), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }

    private sealed record CreateDelegationBody(
        Guid DelegateUserId,
        Guid? ProjectOid,
        Guid? SubcontractorId,
        string? ApproverRole,
        DateTimeOffset ValidFrom,
        DateTimeOffset ValidTo);
}
