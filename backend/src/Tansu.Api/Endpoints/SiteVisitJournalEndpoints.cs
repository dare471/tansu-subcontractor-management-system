using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Application.SiteVisitJournal;

namespace Tansu.Api.Endpoints;

public static class SiteVisitJournalEndpoints
{
    public static IEndpointRouteBuilder MapSiteVisitJournalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/site-visit-journal", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] string? search,
            [FromQuery] Guid? subcontractorId,
            [FromQuery] Guid? projectOid,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListSiteVisitJournalQuery(
                page, pageSize, search, subcontractorId, projectOid, from, to), ct);
            return Results.Ok(result);
        })
        .WithTags("Site visit journal")
        .RequireAuthorization()
        .WithSummary("Журнал посещений с учётом роли и привязок.");

        app.MapGet("/api/site-visit-journal/export", async (
            [FromQuery] string format,
            [FromQuery] string? search,
            [FromQuery] Guid? subcontractorId,
            [FromQuery] Guid? projectOid,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var file = await mediator.Send(new ExportSiteVisitJournalQuery(
                format, search, subcontractorId, projectOid, from, to), ct);
            return Results.File(file.Content, file.ContentType, file.FileName);
        })
        .WithTags("Site visit journal")
        .RequireAuthorization()
        .WithSummary("Экспорт журнала посещений (Excel/CSV или PDF).");

        return app;
    }
}
