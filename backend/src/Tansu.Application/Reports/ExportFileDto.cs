namespace Tansu.Application.Reports;

public sealed record ExportFileDto(byte[] Content, string ContentType, string FileName);
