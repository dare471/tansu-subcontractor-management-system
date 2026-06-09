using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Tansu.Application.Reports;

public static class PdfReportWriter
{
    static PdfReportWriter() => QuestPDF.Settings.License = LicenseType.Community;

    public static ExportFileDto Build(
        string title,
        string fileNameStem,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Text(title).SemiBold().FontSize(14);
                page.Content().PaddingVertical(12).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        for (var i = 0; i < headers.Count; i++)
                            c.RelativeColumn();
                    });
                    table.Header(h =>
                    {
                        foreach (var head in headers)
                            h.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(head).SemiBold();
                    });
                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell);
                    }
                });
                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("Сформировано автоматически — ");
                    t.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                });
            });
        });

        var bytes = document.GeneratePdf();
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        return new ExportFileDto(bytes, "application/pdf", $"{fileNameStem}-{stamp}.pdf");
    }
}
