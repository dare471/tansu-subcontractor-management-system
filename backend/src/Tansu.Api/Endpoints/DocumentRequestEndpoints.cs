using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.Approvals;
using Tansu.Application.DocumentRequests;
using Tansu.Application.DocumentRequests.Commands;
using Tansu.Application.DocumentRequests.Queries;

namespace Tansu.Api.Endpoints;

public static class DocumentRequestEndpoints
{
    public static IEndpointRouteBuilder MapDocumentRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/api/document-requests")
            .WithTags("DocumentRequests")
            .RequireAuthorization();

        requests.MapGet("", async (
            [FromQuery] string? requestType,
            [FromQuery] string? search,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListDocumentRequestsQuery(requestType, search), ct)));

        requests.MapPost("", async (
            [FromBody] CreateDocumentRequestRequest body,
            IMediator m, CancellationToken ct) =>
        {
            var dto = await m.Send(new CreateDocumentRequestCommand(
                body.ProjectOid, body.RequestType, body.Title, body.Description), ct);
            return Results.Created($"/api/document-requests/{dto.Id}", dto);
        }).RequireAuthorization(AuthPolicies.SubcontractorOnly);

        requests.MapPut("/{id:guid}", async (
            Guid id, [FromBody] UpdateDocumentRequestRequest body,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new UpdateDocumentRequestCommand(id, body.Title, body.Description), ct)))
            .RequireAuthorization(AuthPolicies.SubcontractorOnly);

        requests.MapDelete("/{id:guid}", async (
            Guid id, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new DeleteDocumentRequestCommand(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(AuthPolicies.SubcontractorOnly);

        requests.MapPost("/{id:guid}/submit", async (
            Guid id, IMediator m, CancellationToken ct) =>
        {
            var roundId = await m.Send(new SubmitDocumentRequestCommand(id), ct);
            return Results.Ok(new { roundId });
        }).RequireAuthorization(AuthPolicies.SubcontractorOnly);

        requests.MapPost("/{id:guid}/resubmit", async (
            Guid id, IMediator m, CancellationToken ct) =>
        {
            var roundId = await m.Send(new ResubmitDocumentRequestCommand(id), ct);
            return Results.Ok(new { roundId });
        }).RequireAuthorization(AuthPolicies.SubcontractorOnly);

        requests.MapGet("/{id:guid}/approvals", async (
            Guid id, IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new GetDocumentRequestApprovalsQuery(id), ct)));

        var inbox = app.MapGroup("/api/document-request-approvals")
            .WithTags("DocumentRequests")
            .RequireAuthorization(AuthPolicies.TansuOnly);

        inbox.MapGet("/inbox", async (IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetDocumentRequestInboxQuery(), ct)));

        inbox.MapPost("/{sheetId:guid}/approve", async (
            Guid sheetId, [FromBody] DecisionRequest? body,
            IMediator m, CancellationToken ct) =>
        {
            await m.Send(new ApproveDocumentRequestCommand(sheetId, body?.Comment), ct);
            return Results.NoContent();
        });

        inbox.MapPost("/{sheetId:guid}/reject", async (
            Guid sheetId, [FromBody] DecisionRequest body,
            IMediator m, CancellationToken ct) =>
        {
            await m.Send(new RejectDocumentRequestCommand(sheetId, body.Comment ?? string.Empty), ct);
            return Results.NoContent();
        });

        var matrix = app.MapGroup("/api/document-matrix")
            .WithTags("DocumentRequests")
            .RequireAuthorization(AuthPolicies.TansuOnly);

        matrix.MapGet("/summaries", async (IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new ListDocumentMatricesQuery(), ct)));

        matrix.MapGet("", async (
            [FromQuery] Guid projectOid,
            [FromQuery] Guid subcontractorId,
            [FromQuery] string requestType,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new GetDocumentMatrixQuery(projectOid, subcontractorId, requestType), ct)));

        matrix.MapPut("", async (
            [FromQuery] Guid projectOid,
            [FromQuery] Guid subcontractorId,
            [FromQuery] string requestType,
            [FromBody] SetDocumentMatrixRequest body,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new SetDocumentMatrixCommand(
                    projectOid, subcontractorId, requestType, body.Steps), ct)));

        return app;
    }
}
