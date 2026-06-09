using MediatR;
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
    public async Task<ExportFileDto> Handle(ExportSiteVisitJournalQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanViewVisitJournal, "Журнал посещений недоступен для вашей роли.");

        return await SiteVisitJournalExportBuilder.BuildAsync(
            db, access, req.Format, req.Search, req.SubcontractorId, req.ProjectOid, req.From, req.To, ct);
    }
}
