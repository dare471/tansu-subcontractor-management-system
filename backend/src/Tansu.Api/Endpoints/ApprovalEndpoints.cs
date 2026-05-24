using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.Approvals;
using Tansu.Application.Approvals.Commands;
using Tansu.Application.Approvals.Queries;

namespace Tansu.Api.Endpoints;

public static class ApprovalEndpoints
{
    public static IEndpointRouteBuilder MapApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        var employees = app.MapGroup("/api/employees").WithTags("Approvals")
            .RequireAuthorization();

        employees.MapPost("/{id:guid}/submit", async (
            Guid id, IMediator m, CancellationToken ct) =>
        {
            var round = await m.Send(new SubmitEmployeeCommand(id), ct);
            return Results.Ok(new { roundId = round });
        }).RequireAuthorization(AuthPolicies.SubcontractorOnly);

        employees.MapPost("/{id:guid}/resubmit", async (
            Guid id, IMediator m, CancellationToken ct) =>
        {
            var round = await m.Send(new ResubmitEmployeeCommand(id), ct);
            return Results.Ok(new { roundId = round });
        }).RequireAuthorization(AuthPolicies.SubcontractorOnly);

        employees.MapGet("/{id:guid}/approvals", async (
            Guid id, IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new GetEmployeeApprovalsQuery(id), ct)));

        var approvals = app.MapGroup("/api/approvals").WithTags("Approvals")
            .RequireAuthorization();

        approvals.MapGet("/inbox", async (IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetInboxQuery(), ct)));

        approvals.MapPost("/{sheetId:guid}/approve", async (
            Guid sheetId, [FromBody] DecisionRequest? body,
            IMediator m, CancellationToken ct) =>
        {
            await m.Send(new ApproveCommand(sheetId, body?.Comment), ct);
            return Results.NoContent();
        });

        approvals.MapPost("/{sheetId:guid}/reject", async (
            Guid sheetId, [FromBody] DecisionRequest body,
            IMediator m, CancellationToken ct) =>
        {
            await m.Send(new RejectCommand(sheetId, body?.Comment ?? string.Empty), ct);
            return Results.NoContent();
        });

        return app;
    }
}
