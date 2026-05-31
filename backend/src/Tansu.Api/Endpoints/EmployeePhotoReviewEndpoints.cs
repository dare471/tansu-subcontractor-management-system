using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Application.EmployeePhotoReviews;
using Tansu.Application.EmployeePhotoReviews.Commands;
using Tansu.Application.EmployeePhotoReviews.Queries;

namespace Tansu.Api.Endpoints;

public static class EmployeePhotoReviewEndpoints
{
    public static IEndpointRouteBuilder MapEmployeePhotoReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/employees")
            .WithTags("Employee photo reviews")
            .RequireAuthorization();

        g.MapGet("/photo-reviews/pending", async (IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListPendingPhotoReviewsQuery(), ct)))
        .WithSummary("Очередь фото на ручную проверку (ТАНСУ).");

        g.MapGet("/{id:guid}/photo-review", async (Guid id, IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new GetEmployeePhotoReviewStatusQuery(id), ct)))
        .WithSummary("Статус проверки фото сотрудника.");

        g.MapPost("/{id:guid}/photo-review/approve", async (
            Guid id,
            [FromBody] ManualPhotoReviewRequest? req,
            IMediator m,
            CancellationToken ct) =>
        {
            var dto = await m.Send(new ManualApproveEmployeePhotoCommand(id, req?.Comment), ct);
            return Results.Ok(dto);
        })
        .WithSummary("Одобрить фото вручную (ТАНСУ).");

        g.MapPost("/{id:guid}/photo-review/reject", async (
            Guid id,
            [FromBody] ManualPhotoRejectRequest req,
            IMediator m,
            CancellationToken ct) =>
        {
            var dto = await m.Send(new ManualRejectEmployeePhotoCommand(id, req.Reason), ct);
            return Results.Ok(dto);
        })
        .WithSummary("Отклонить фото с указанием причины (ТАНСУ).");

        return app;
    }
}

public sealed record ManualPhotoRejectRequest(string Reason);
