using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.EmployeePortal;
using Tansu.Application.EmployeePortal.Commands;
using Tansu.Application.EmployeePortal.Queries;
using Tansu.Application.EmployeeDocuments.Queries;
using Tansu.Application.PpeIssuance.Queries;

namespace Tansu.Api.Endpoints;

public static class EmployeePortalEndpoints
{
    public static IEndpointRouteBuilder MapEmployeePortalEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/employee/login", async (
            [FromBody] EmployeeLoginRequest req,
            IMediator mediator,
            CancellationToken ct) =>
                Results.Ok(await mediator.Send(new EmployeeLoginCommand(req.Iin, req.Password), ct)))
        .AllowAnonymous()
        .WithSummary("Вход сотрудника в личный кабинет (ИИН + пароль).");

        var portal = app.MapGroup("/api/employee-portal")
            .WithTags("EmployeePortal")
            .RequireAuthorization(AuthPolicies.EmployeeOnly);

        portal.MapGet("/dashboard", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetEmployeePortalDashboardQuery(), ct)))
        .WithSummary("Данные личного кабинета сотрудника.");

        portal.MapGet("/profile", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetEmployeePortalProfileQuery(), ct)))
        .WithSummary("Профиль сотрудника.");

        portal.MapGet("/approvals", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetEmployeePortalApprovalsQuery(), ct)))
        .WithSummary("Статус и история согласования (только свой профиль).");

        portal.MapGet("/site-visits", async (
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var p = page ?? 1;
            var ps = pageSize ?? 50;
            return Results.Ok(await mediator.Send(new GetEmployeePortalSiteVisitsQuery(
                p <= 0 ? 1 : p, ps <= 0 ? 50 : ps, from, to), ct));
        })
        .WithSummary("Журнал проходов на объект.");

        portal.MapGet("/ppe", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetEmployeePortalPpeQuery(), ct)))
        .WithSummary("Выданные СИЗ (каска, униформа).");

        portal.MapGet("/documents", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetEmployeePortalDocumentsQuery(), ct)))
        .WithSummary("Документы сотрудника (read-only).");

        portal.MapGet("/documents/{documentId:guid}/file", async (
            Guid documentId, IMediator mediator, CancellationToken ct) =>
        {
            var file = await mediator.Send(new GetEmployeePortalDocumentFileQuery(documentId), ct);
            return file is null
                ? Results.NotFound()
                : Results.File(file.Value.Stream, file.Value.ContentType, file.Value.FileName);
        })
        .WithSummary("Скачать свой документ.");

        portal.MapGet("/blocks", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetEmployeePortalBlockStatusQuery(), ct)))
        .WithSummary("Статус блокировки (read-only).");

        portal.MapPost("/photo", async (HttpRequest http, IMediator mediator, CancellationToken ct) =>
        {
            if (!http.HasFormContentType)
                return Results.BadRequest(new { code = "bad_request", detail = "Ожидается multipart/form-data." });

            var form = await http.ReadFormAsync(ct);
            var file = form.Files["file"] ?? form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { code = "bad_request", detail = "Файл не передан." });
            if (file.Length > 200 * 1024)
                return Results.BadRequest(new { code = "file_too_large", detail = "Файл больше 200 КБ (требование Hikvision)." });

            await using var stream = file.OpenReadStream();
            var result = await mediator.Send(new UploadEmployeePortalPhotoCommand(file.FileName, stream), ct);
            return Results.Ok(result);
        })
        .DisableAntiforgery()
        .WithSummary("Загрузить фото для Face ID.");

        portal.MapGet("/photo", async (IMediator mediator, CancellationToken ct) =>
        {
            var stream = await mediator.Send(new GetEmployeePortalPhotoQuery(), ct);
            return stream is null ? Results.NotFound() : Results.File(stream, "image/jpeg");
        })
        .WithSummary("Фото сотрудника для Face ID.");

        portal.MapGet("/safety-quiz", async (
            [FromQuery] string? locale,
            IMediator mediator,
            CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetSafetyQuizQuery(locale), ct)))
        .WithSummary("Вопросы опроса по ТБ.");

        portal.MapPost("/safety-quiz", async (
            [FromBody] SafetyQuizSubmitRequest req,
            IMediator mediator,
            CancellationToken ct) =>
                Results.Ok(await mediator.Send(new SubmitSafetyQuizCommand(req.Answers), ct)))
        .WithSummary("Отправить ответы опроса по ТБ.");

        portal.MapGet("/access-pass/qr.png", async (IMediator mediator, CancellationToken ct) =>
        {
            var png = await mediator.Send(new GetEmployeePortalQrQuery(), ct);
            return png is null ? Results.NotFound() : Results.File(png, "image/png");
        })
        .WithSummary("QR-пропуск (доступен после опроса по ТБ).");

        return app;
    }
}
