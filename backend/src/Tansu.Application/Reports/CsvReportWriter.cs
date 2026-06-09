using System.Globalization;
using System.Text;

namespace Tansu.Application.Reports;

public static class CsvReportWriter
{
    public static ExportFileDto Build(string fileNameStem, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', headers.Select(Csv)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(';', row.Select(Csv)));

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return new ExportFileDto(bytes, "text/csv; charset=utf-8", $"{fileNameStem}-{stamp}.csv");
    }

    public static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
