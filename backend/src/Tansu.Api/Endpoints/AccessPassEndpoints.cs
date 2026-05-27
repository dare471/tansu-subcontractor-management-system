using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tansu.Application.AccessPasses;
using Tansu.Application.AccessPasses.Commands;
using Tansu.Application.AccessPasses.Queries;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Api.Endpoints;

public static class AccessPassEndpoints
{
    public static IEndpointRouteBuilder MapAccessPassEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/employees/{employeeId:guid}/access-pass")
            .WithTags("AccessPass")
            .RequireAuthorization();

        g.MapGet("", async (Guid employeeId, IMediator mediator, CancellationToken ct) =>
        {
            var pass = await mediator.Send(new GetEmployeeAccessPassQuery(employeeId), ct);
            return pass is null ? Results.NotFound() : Results.Ok(pass);
        })
        .WithSummary("Активный QR-пропуск сотрудника (после полного согласования).");

        g.MapGet("/qr.png", async (Guid employeeId, IMediator mediator, CancellationToken ct) =>
        {
            var png = await mediator.Send(new GetAccessPassQrQuery(employeeId), ct);
            return png is null ? Results.NotFound() : Results.File(png, "image/png");
        })
        .WithSummary("QR-код пропуска (PNG).");

        return app;
    }

    public static IEndpointRouteBuilder MapInternalVerifyEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/internal/access-passes").WithTags("InternalVerify");

        g.MapGet("/{token}", async (
            string token,
            HttpContext ctx,
            IMediator mediator,
            IOptions<AccessPassOptions> options,
            CancellationToken ct) =>
        {
            if (!VerifyServiceKeyMatches(ctx, options.Value.VerifyServiceKey))
                return Results.Unauthorized();

            var lookup = await mediator.Send(new GetAccessPassByTokenQuery(token), ct);
            return lookup is null ? Results.NotFound() : Results.Ok(lookup);
        })
        .AllowAnonymous()
        .WithSummary("Данные пропуска по токену (для сервиса проверки).");

        g.MapGet("/{token}/reference-photo", async (
            string token,
            HttpContext ctx,
            IMediator mediator,
            IOptions<AccessPassOptions> options,
            ITansuDbContext db,
            IPhotoStorage storage,
            CancellationToken ct) =>
        {
            if (!VerifyServiceKeyMatches(ctx, options.Value.VerifyServiceKey))
                return Results.Unauthorized();

            var lookup = await mediator.Send(new GetAccessPassByTokenQuery(token), ct);
            if (lookup is null || !lookup.IsActive || !lookup.HasReferencePhoto)
                return Results.NotFound();

            var employee = await db.Employees.FindAsync([lookup.EmployeeId], ct);
            if (employee is null || string.IsNullOrEmpty(employee.PhotoPath))
                return Results.NotFound();

            var stream = await storage.OpenReadAsync(employee.PhotoPath, ct);
            return stream is null ? Results.NotFound() : Results.File(stream, "image/jpeg");
        })
        .AllowAnonymous()
        .WithSummary("Эталонное фото сотрудника для Face ID (для сервиса проверки).");

        g.MapPost("/{token}/check-in", async (
            string token,
            HttpContext ctx,
            IMediator mediator,
            IOptions<AccessPassOptions> options,
            [FromBody] RecordSiteVisitRequest? body,
            CancellationToken ct) =>
        {
            if (!VerifyServiceKeyMatches(ctx, options.Value.VerifyServiceKey))
                return Results.Unauthorized();

            var visit = await mediator.Send(
                new RecordEmployeeSiteVisitCommand(token, body?.FaceConfidence ?? 0),
                ct);
            return visit is null ? Results.NotFound() : Results.Ok(visit);
        })
        .AllowAnonymous()
        .WithSummary("Записать проход на объект после успешного Face ID.");

        return app;
    }

    private sealed record RecordSiteVisitRequest(double FaceConfidence);

    private static bool VerifyServiceKeyMatches(HttpContext ctx, string expectedKey)
    {
        if (string.IsNullOrWhiteSpace(expectedKey))
            return false;

        var provided = ctx.Request.Headers["X-Tansu-Verify-Key"].FirstOrDefault();
        return string.Equals(provided, expectedKey, StringComparison.Ordinal);
    }
}
