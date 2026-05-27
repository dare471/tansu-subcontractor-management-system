using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.EmployeePortal;
using Tansu.Application.EmployeePortal.Commands;
using Tansu.Application.EmployeePortal.Queries;
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

        portal.MapGet("/site-visits", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetEmployeePortalSiteVisitsQuery(), ct)))
        .WithSummary("Журнал проходов на объект.");

        portal.MapGet("/ppe", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetEmployeePortalPpeQuery(), ct)))
        .WithSummary("Выданные СИЗ (каска, униформа).");

        portal.MapPost("/photo", async (HttpRequest http, IMediator mediator, CancellationToken ct) =>
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

        portal.MapGet("/safety-quiz", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetSafetyQuizQuery(), ct)))
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
