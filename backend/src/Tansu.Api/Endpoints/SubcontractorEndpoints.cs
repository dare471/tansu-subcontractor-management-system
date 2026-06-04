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
                Results.Ok(await m.Send(new UpdateSubcontractorCommand(id, req.Name, req.Bin, req.ManagerUserId), ct)));

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

        g.MapGet("/{id:guid}/documents", async (
            Guid id, IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListSubcontractorDocumentsQuery(id), ct)));

        g.MapPost("/{id:guid}/documents", async (
            Guid id, HttpRequest request, IMediator m, CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { detail = "Ожидается multipart/form-data." });

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { detail = "Файл не передан." });

            var name = form["name"].ToString();
            var docType = form["documentType"].ToString();
            await using var stream = file.OpenReadStream();
            var dto = await m.Send(new UploadSubcontractorDocumentCommand(
                id, name, docType, file.FileName, stream), ct);
            return Results.Created($"/api/subcontractors/{id}/documents/{dto.Id}", dto);
        });

        g.MapGet("/{id:guid}/documents/{documentId:guid}", async (
            Guid id, Guid documentId, IMediator m, CancellationToken ct) =>
        {
            var file = await m.Send(new GetSubcontractorDocumentFileQuery(id, documentId), ct);
            if (file is null) return Results.NotFound();
            return Results.File(file.Value.Stream, file.Value.ContentType, file.Value.FileName);
        });

        g.MapDelete("/{id:guid}/documents/{documentId:guid}", async (
            Guid id, Guid documentId, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new DeleteSubcontractorDocumentCommand(id, documentId), ct);
            return Results.NoContent();
        });

        return app;
    }
}
