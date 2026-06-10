using System.Text;
using FluentAssertions;
using Tansu.Application.Reports;

namespace Tansu.UnitTests;

public sealed class CsvReportWriterTests
{
    [Fact]
    public void Build_writes_utf8_bom_and_semicolon_delimiter()
    {
        var file = CsvReportWriter.Build(
            "test-report",
            ["Колонка A", "Колонка B"],
            [["значение;с точкой", "простое"]]);

        file.ContentType.Should().Contain("csv");
        file.FileName.Should().StartWith("test-report-").And.EndWith(".csv");

        file.Content[0].Should().Be(0xEF);
        file.Content[1].Should().Be(0xBB);
        file.Content[2].Should().Be(0xBF);

        var text = Encoding.UTF8.GetString(file.Content, 3, file.Content.Length - 3);
        text.Should().Contain("Колонка A;Колонка B");
        text.Should().Contain("\"значение;с точкой\";простое");
    }

    [Fact]
    public void Csv_escapes_special_characters()
    {
        CsvReportWriter.Csv("plain").Should().Be("plain");
        CsvReportWriter.Csv(null).Should().Be("");
        CsvReportWriter.Csv("a;b").Should().Be("\"a;b\"");
        CsvReportWriter.Csv("line\nbreak").Should().Be("\"line\nbreak\"");
    }
}
