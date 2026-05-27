using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Application.AccessPasses.Queries;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Application.Employees.Commands;
using Tansu.Application.Employees.Queries;
using Tansu.Application.PpeIssuance;
using Tansu.Application.PpeIssuance.Commands;
using Tansu.Application.PpeIssuance.Queries;
using Tansu.Domain.Enums;

namespace Tansu.Api.Endpoints;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/employees")
            .WithTags("Employees")
            .RequireAuthorization();

        g.MapGet("", async (
            [FromQuery] Guid? projectOid,
            [FromQuery] Guid? subcontractorId,
            [FromQuery] string? search,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListEmployeesQuery(projectOid, subcontractorId, search), ct)));

        g.MapPost("", async (
            [FromBody] CreateEmployeeRequest req,
            ICurrentUser currentUser,
            IMediator m, CancellationToken ct) =>
        {
            var sid = currentUser.UserType == UserType.Subcontractor
                ? currentUser.SubcontractorId
                  ?? throw new ForbiddenException("Контекст субподрядчика отсутствует.")
                : throw new ForbiddenException("Создавать сотрудников может только субподрядчик.");

            var dto = await m.Send(new CreateEmployeeCommand(
                sid, req.ProjectOid, req.FullName, req.Position, req.Phone, req.Iin), ct);
            return Results.Created($"/api/employees/{dto.Id}", dto);
        });

        g.MapPut("/{id:guid}", async (
            Guid id, [FromBody] UpdateEmployeeRequest req,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new UpdateEmployeeCommand(
                    id, req.FullName, req.Position, req.Phone, req.Iin), ct)));

        g.MapDelete("/{id:guid}", async (
            Guid id, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new DeleteEmployeeCommand(id), ct);
            return Results.NoContent();
        });

        g.MapPost("/{id:guid}/photo", async (
            Guid id, HttpRequest http,
            IMediator m, CancellationToken ct) =>
        {
            if (!http.HasFormContentType)
                return Results.BadRequest(new { code = "bad_request", detail = "Ожидается multipart/form-data." });

            var form = await http.ReadFormAsync(ct);
            var file = form.Files["file"] ?? form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { code = "bad_request", detail = "Файл не передан." });
            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest(new { code = "file_too_large", detail = "Файл больше 5 МБ." });

            await using var stream = file.OpenReadStream();
            var relative = await m.Send(new UploadPhotoCommand(id, file.FileName, stream), ct);
            return Results.Ok(new { photoPath = relative });
        })
        .DisableAntiforgery();

        g.MapGet("/{id:guid}/photo", async (
            Guid id, IPhotoStorage storage, ITansuDbContext db, CancellationToken ct) =>
        {
            var e = await db.Employees.FindAsync([id], ct);
            if (e is null || string.IsNullOrEmpty(e.PhotoPath)) return Results.NotFound();
            var stream = await storage.OpenReadAsync(e.PhotoPath, ct);
            return stream is null ? Results.NotFound() : Results.Stream(stream, "image/*");
        });

        g.MapGet("/{id:guid}/site-visits", async (
            Guid id, IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new GetEmployeeSiteVisitsQuery(id), ct)))
        .WithSummary("История проходов сотрудника на объект (Face ID).");

        g.MapGet("/{id:guid}/ppe", async (
            Guid id, IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new GetEmployeePpeSummaryQuery(id), ct)))
        .WithSummary("Выданные СИЗ сотрудника.");

        g.MapPost("/{id:guid}/ppe", async (
            Guid id, [FromBody] IssueEmployeePpeRequest req, IMediator m, CancellationToken ct) =>
        {
            var dto = await m.Send(new IssueEmployeePpeCommand(
                id, req.ItemType, req.Size, req.InventoryNumber, req.Notes), ct);
            return Results.Created($"/api/employees/{id}/ppe/{dto.Id}", dto);
        })
        .WithSummary("Выдать каску или униформу.");

        g.MapPost("/{id:guid}/ppe/{issuanceId:guid}/return", async (
            Guid id, Guid issuanceId, [FromBody] ReturnEmployeePpeRequest req,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ReturnEmployeePpeCommand(id, issuanceId, req.Notes), ct)))
        .WithSummary("Оформить возврат СИЗ.");

        return app;
    }
}
