using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.SiteVisitJournal;

public sealed record ExportSiteVisitJournalQuery(
    string Format,
    string? Search = null,
    Guid? SubcontractorId = null,
    Guid? ProjectOid = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<ExportFileDto>;

public sealed record ExportFileDto(byte[] Content, string ContentType, string FileName);

public sealed class ExportSiteVisitJournalHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<ExportSiteVisitJournalQuery, ExportFileDto>
{
    private const int MaxRows = 10_000;

    public async Task<ExportFileDto> Handle(ExportSiteVisitJournalQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanViewVisitJournal, "Журнал посещений недоступен для вашей роли.");

        var q = SiteVisitJournalQueryBuilder.ApplyFilters(
            db.EmployeeSiteVisits.AsNoTracking(),
            access,
            req.Search,
            req.SubcontractorId,
            req.ProjectOid,
            req.From,
            req.To);

        var rows = await q
            .Include(v => v.Employee!)
            .ThenInclude(e => e!.Subcontractor)
            .Include(v => v.Employee!)
            .ThenInclude(e => e!.Project)
            .OrderByDescending(v => v.CheckedInAt)
            .Take(MaxRows)
            .ToListAsync(ct);

        var format = req.Format.Trim().ToLowerInvariant();
        return format switch
        {
            "pdf" => BuildPdf(rows),
            _ => BuildCsv(rows)
        };
    }

    private static ExportFileDto BuildCsv(IReadOnlyList<Domain.Entities.EmployeeSiteVisit> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ФИО;Субподрядчик;Объект;Локация терминала;Вход;Выход;Источник;Face ID");
        foreach (var v in rows)
        {
            var emp = v.Employee!;
            sb.AppendLine(string.Join(';', new[]
            {
                Csv(emp.FullName),
                Csv(emp.Subcontractor!.Name),
                Csv(emp.Project?.Name ?? "—"),
                Csv(v.TerminalLocation ?? "—"),
                Csv(v.CheckedInAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")),
                Csv(v.CheckedOutAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") ?? "—"),
                Csv(Domain.Enums.SiteVisitDataSource.Label(v.DataSource)),
                Csv(v.FaceConfidence?.ToString("P1", CultureInfo.InvariantCulture) ?? "—")
            }));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        return new ExportFileDto(bytes, "text/csv; charset=utf-8", $"journal-poseshcheniy-{stamp}.csv");
    }

    private static ExportFileDto BuildPdf(IReadOnlyList<Domain.Entities.EmployeeSiteVisit> rows)
    {
        var lines = new List<string>
        {
            "Журнал посещений TANSU",
            $"Экспорт: {DateTime.Now:dd.MM.yyyy HH:mm}",
            $"Записей: {rows.Count}",
            ""
        };

        foreach (var v in rows)
        {
            var emp = v.Employee!;
            lines.Add($"• {emp.FullName} | {emp.Subcontractor!.Name} | {emp.Project?.Name ?? "—"}");
            lines.Add($"  Вход: {v.CheckedInAt.ToLocalTime():dd.MM.yyyy HH:mm:ss}" +
                        (v.CheckedOutAt is { } outAt ? $"  Выход: {outAt.ToLocalTime():dd.MM.yyyy HH:mm:ss}" : ""));
            lines.Add($"  Терминал: {v.TerminalLocation ?? "—"}  Источник: {Domain.Enums.SiteVisitDataSource.Label(v.DataSource)}");
            lines.Add("");
        }

        var pdf = SimplePdfBuilder.Build(lines);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        return new ExportFileDto(pdf, "application/pdf", $"journal-poseshcheniy-{stamp}.pdf");
    }

    private static string Csv(string value) =>
        value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}

internal static class SimplePdfBuilder
{
    public static byte[] Build(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 10 Tf");
        var y = 800;
        foreach (var line in lines)
        {
            if (y < 40)
                break;
            content.AppendLine($"1 0 0 1 40 {y} Tm");
            content.AppendLine($"({Escape(line)}) Tj");
            y -= 14;
        }
        content.AppendLine("ET");

        var stream = content.ToString();
        var streamBytes = Encoding.UTF8.GetBytes(stream);

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {streamBytes.Length} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        var pdf = new StringBuilder();
        pdf.AppendLine("%PDF-1.4");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.UTF8.GetByteCount(pdf.ToString()));
            pdf.AppendLine($"{i + 1} 0 obj");
            pdf.AppendLine(objects[i]);
            pdf.AppendLine("endobj");
        }

        var xrefPos = Encoding.UTF8.GetByteCount(pdf.ToString());
        pdf.AppendLine("xref");
        pdf.AppendLine($"0 {objects.Count + 1}");
        pdf.AppendLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
            pdf.AppendLine($"{offset:D10} 00000 n ");
        pdf.AppendLine("trailer");
        pdf.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(xrefPos.ToString());
        pdf.AppendLine("%%EOF");

        return Encoding.UTF8.GetBytes(pdf.ToString());
    }

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
