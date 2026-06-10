using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Reports;

namespace Tansu.Application.SiteVisitJournal;

internal static class SiteVisitJournalExportBuilder
{
    private const int MaxRows = 10_000;

    public static async Task<ExportFileDto> BuildAsync(
        ITansuDbContext db,
        TansuAccessContext access,
        string format,
        string? search,
        Guid? subcontractorId,
        Guid? projectOid,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        var q = SiteVisitJournalQueryBuilder.ApplyFilters(
            db.EmployeeSiteVisits.AsNoTracking(),
            access,
            search,
            subcontractorId,
            projectOid,
            from,
            to);

        var rows = await q
            .Include(v => v.Employee!)
            .ThenInclude(e => e!.Subcontractor)
            .Include(v => v.Employee!)
            .ThenInclude(e => e!.Project)
            .OrderByDescending(v => v.CheckedInAt)
            .Take(MaxRows)
            .ToListAsync(ct);

        return format.Trim().ToLowerInvariant() switch
        {
            "pdf" => BuildPdf(rows),
            _ => BuildCsv(rows)
        };
    }

    private static ExportFileDto BuildCsv(IReadOnlyList<Domain.Entities.EmployeeSiteVisit> rows)
    {
        var headers = new[] { "ФИО", "Субподрядчик", "Объект", "Локация терминала", "Вход", "Выход", "Источник", "Face ID" };
        var data = rows.Select(v =>
        {
            var emp = v.Employee!;
            return (IReadOnlyList<string>)[
                emp.FullName,
                emp.Subcontractor!.Name,
                emp.Project?.Name ?? "—",
                v.TerminalLocation ?? "—",
                v.CheckedInAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"),
                v.CheckedOutAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") ?? "—",
                Domain.Enums.SiteVisitDataSource.Label(v.DataSource),
                v.FaceConfidence?.ToString("P1", CultureInfo.InvariantCulture) ?? "—"
            ];
        });
        return MapExport(CsvReportWriter.Build("journal-poseshcheniy", headers, data));
    }

    private static ExportFileDto BuildPdf(IReadOnlyList<Domain.Entities.EmployeeSiteVisit> rows)
    {
        var headers = new[] { "ФИО", "Субподрядчик", "Объект", "Вход", "Выход", "Источник" };
        var data = rows.Select(v =>
        {
            var emp = v.Employee!;
            return (IReadOnlyList<string>)[
                emp.FullName,
                emp.Subcontractor!.Name,
                emp.Project?.Name ?? "—",
                v.CheckedInAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                v.CheckedOutAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "—",
                Domain.Enums.SiteVisitDataSource.Label(v.DataSource)
            ];
        }).ToList();
        return MapExport(PdfReportWriter.Build("Журнал посещений", "journal-poseshcheniy", headers, data));
    }

    private static ExportFileDto MapExport(Reports.ExportFileDto f) =>
        new(f.Content, f.ContentType, f.FileName);
}
