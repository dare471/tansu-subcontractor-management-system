using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Application.Reports;

namespace Tansu.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/approved-personnel/export", ExportApprovedPersonnel);
        app.MapGet("/api/reports/site-visits/export", ExportSiteVisits);
        app.MapGet("/api/reports/employee-blocks/export", ExportBlocks);
        app.MapGet("/api/reports/document-requests/export", ExportDocRequests);
        app.MapGet("/api/reports/expiring-documents/export", ExportExpiring);
        app.MapGet("/api/reports/subcontractor-compliance", GetCompliance);
        return app;
    }

    private static async Task<IResult> ExportApprovedPersonnel(
        [FromQuery] string format,
        [FromQuery] Guid? projectOid,
        [FromQuery] Guid? subcontractorId,
        [FromQuery] DateTimeOffset? asOfDate,
        IMediator mediator, CancellationToken ct)
    {
        var file = await mediator.Send(new ExportApprovedPersonnelQuery(format, projectOid, subcontractorId, asOfDate), ct);
        return Results.File(file.Content, file.ContentType, file.FileName);
    }

    private static async Task<IResult> ExportSiteVisits(
        [FromQuery] string format,
        [FromQuery] string? search,
        [FromQuery] Guid? subcontractorId,
        [FromQuery] Guid? projectOid,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        IMediator mediator, CancellationToken ct)
    {
        var file = await mediator.Send(new ExportSiteVisitsReportQuery(format, search, subcontractorId, projectOid, from, to), ct);
        return Results.File(file.Content, file.ContentType, file.FileName);
    }

    private static async Task<IResult> ExportBlocks(
        [FromQuery] string format,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] Guid? projectOid,
        [FromQuery] Guid? subcontractorId,
        IMediator mediator, CancellationToken ct)
    {
        var file = await mediator.Send(new ExportEmployeeBlocksQuery(format, from, to, projectOid, subcontractorId), ct);
        return Results.File(file.Content, file.ContentType, file.FileName);
    }

    private static async Task<IResult> ExportDocRequests(
        [FromQuery] string format,
        [FromQuery] string? status,
        [FromQuery] string? requestType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        IMediator mediator, CancellationToken ct)
    {
        var file = await mediator.Send(new ExportDocumentRequestsQuery(format, status, requestType, from, to), ct);
        return Results.File(file.Content, file.ContentType, file.FileName);
    }

    private static async Task<IResult> ExportExpiring(
        [FromQuery] string format,
        [FromQuery] int? daysAhead,
        IMediator mediator, CancellationToken ct)
    {
        var days = daysAhead ?? 14;
        var file = await mediator.Send(new ExportExpiringDocumentsQuery(format, days <= 0 ? 14 : days), ct);
        return Results.File(file.Content, file.ContentType, file.FileName);
    }

    private static async Task<IResult> GetCompliance(
        [FromQuery] Guid? subcontractorId,
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSubcontractorComplianceQuery(subcontractorId), ct);
        return Results.Ok(result);
    }
}
