using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.Projects;
using Tansu.Application.Projects.Commands;
using Tansu.Application.Projects.Queries;
using Tansu.Application.Subcontractors;
using Tansu.Application.Subcontractors.Commands;

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

        g.MapGet("/bind-options", async (IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListProjectBindOptionsQuery(), ct)))
            .RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapGet("/staff-options", async (IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListProjectStaffOptionsQuery(), ct)))
            .RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapGet("/{projectOid:guid}", async (
            Guid projectOid, IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new GetProjectDetailQuery(projectOid), ct)))
            .RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapPut("/{projectOid:guid}", async (
            Guid projectOid,
            [FromBody] UpdateProjectRequest body,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new UpdateProjectCommand(projectOid, body), ct)))
            .RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapPost("", async (
            [FromBody] RegisterProjectCommand cmd,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(cmd, ct)))
            .RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapPost("/{projectOid:guid}/documents", async (
            Guid projectOid,
            HttpRequest request,
            IMediator m,
            CancellationToken ct) =>
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
            var dto = await m.Send(new UploadProjectDocumentCommand(
                projectOid, name, docType, file.FileName, stream), ct);
            return Results.Created($"/api/projects/{projectOid}/documents/{dto.Id}", dto);
        }).RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapGet("/{projectOid:guid}/documents/{documentId:guid}", async (
            Guid projectOid,
            Guid documentId,
            IMediator m,
            CancellationToken ct) =>
        {
            var file = await m.Send(new GetProjectDocumentFileQuery(projectOid, documentId), ct);
            if (file is null) return Results.NotFound();
            return Results.File(file.Value.Stream, file.Value.ContentType, file.Value.FileName);
        }).RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapDelete("/{projectOid:guid}/documents/{documentId:guid}", async (
            Guid projectOid,
            Guid documentId,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new DeleteProjectDocumentCommand(projectOid, documentId), ct);
            return Results.NoContent();
        }).RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapPost("/{projectOid:guid}/subcontractors", async (
            Guid projectOid,
            [FromBody] BindProjectFromProjectRequest body,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new BindProjectCommand(
                body.SubcontractorId, projectOid, null, body.ActivityType), ct);
            return Results.NoContent();
        }).RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapPut("/{projectOid:guid}/subcontractors/{subcontractorId:guid}", async (
            Guid projectOid,
            Guid subcontractorId,
            [FromBody] UpdateProjectSubcontractorBindingRequest body,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new UpdateProjectSubcontractorBindingCommand(
                projectOid, subcontractorId, body.ActivityType), ct);
            return Results.NoContent();
        }).RequireAuthorization(AuthPolicies.TansuOnly);

        return app;
    }
}
