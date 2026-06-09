using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.DocumentRequests;
using Tansu.Application.SiteVisitJournal;
using Tansu.Domain.Enums;

namespace Tansu.Application.Reports;

public sealed record SubcontractorComplianceDto(
    Guid SubcontractorId,
    string SubcontractorName,
    int TotalEmployees,
    int ApprovedEmployees,
    int BlockedEmployees,
    int QuizCompleted,
    int ExpiringDocuments);

public sealed record ExportApprovedPersonnelQuery(
    string Format,
    Guid? ProjectOid,
    Guid? SubcontractorId,
    DateTimeOffset? AsOfDate) : IRequest<ExportFileDto>;

public sealed record ExportEmployeeBlocksQuery(
    string Format,
    DateTimeOffset? From,
    DateTimeOffset? To,
    Guid? ProjectOid,
    Guid? SubcontractorId) : IRequest<ExportFileDto>;

public sealed record ExportDocumentRequestsQuery(
    string Format,
    string? Status,
    string? RequestType,
    DateTimeOffset? From,
    DateTimeOffset? To) : IRequest<ExportFileDto>;

public sealed record ExportExpiringDocumentsQuery(
    string Format,
    int DaysAhead) : IRequest<ExportFileDto>;

public sealed record GetSubcontractorComplianceQuery(Guid? SubcontractorId) : IRequest<IReadOnlyList<SubcontractorComplianceDto>>;

public sealed class ExportApprovedPersonnelHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<ExportApprovedPersonnelQuery, ExportFileDto>
{
    private const int MaxRows = 10_000;

    public async Task<ExportFileDto> Handle(ExportApprovedPersonnelQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanViewReports, "Экспорт недоступен для вашей роли.");

        var asOf = req.AsOfDate ?? DateTimeOffset.UtcNow;
        var q = db.Employees.AsNoTracking()
            .Include(e => e.Subcontractor)
            .Include(e => e.Project)
            .AsQueryable();
        if (req.ProjectOid is Guid pid) q = q.Where(e => e.ProjectOid == pid);
        if (req.SubcontractorId is Guid sid) q = q.Where(e => e.SubcontractorId == sid);

        var employees = await q.OrderBy(e => e.FullName).Take(MaxRows).ToListAsync(ct);
        var ids = employees.Select(e => e.Id).ToList();
        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeId))
            .ToListAsync(ct);
        var blocked = await db.EmployeeBlockRecords.AsNoTracking()
            .Where(b => ids.Contains(b.EmployeeId) && b.ActionType == EmployeeBlockActionType.Block)
            .GroupBy(b => b.EmployeeId)
            .Select(g => g.Key)
            .ToListAsync(ct);
        var blockedSet = blocked.ToHashSet();

        var headers = new[] { "ФИО", "Субподрядчик", "Объект", "Статус", "На дату" };
        var rows = new List<IReadOnlyList<string>>();
        foreach (var e in employees)
        {
            var empSheets = sheets.Where(s => s.EmployeeId == e.Id).ToList();
            var status = Employees.EmployeeStatusResolver.ResolveFromSheets(empSheets);
            if (status != ApprovalStatus.Approved || blockedSet.Contains(e.Id)) continue;
            rows.Add([
                e.FullName,
                e.Subcontractor?.Name ?? "—",
                e.Project?.Name ?? "—",
                "Допущен",
                asOf.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            ]);
        }
        return Build(req.Format, "dopushchennyj-personal", "Допущенный персонал", headers, rows);
    }

    private static ExportFileDto Build(string format, string stem, string title, IReadOnlyList<string> headers, List<IReadOnlyList<string>> rows) =>
        format.Trim().ToLowerInvariant() == "pdf"
            ? PdfReportWriter.Build(title, stem, headers, rows)
            : CsvReportWriter.Build(stem, headers, rows);
}

public sealed class ExportEmployeeBlocksHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<ExportEmployeeBlocksQuery, ExportFileDto>
{
    public async Task<ExportFileDto> Handle(ExportEmployeeBlocksQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanViewReports, "Экспорт недоступен.");

        var q = db.EmployeeBlockRecords.AsNoTracking()
            .Include(b => b.Employee!).ThenInclude(e => e!.Subcontractor)
            .Include(b => b.Employee!).ThenInclude(e => e!.Project)
            .Include(b => b.InitiatedBy)
            .AsQueryable();
        if (req.From is DateTimeOffset from) q = q.Where(b => b.CreatedAt >= from);
        if (req.To is DateTimeOffset to) q = q.Where(b => b.CreatedAt <= to);
        if (req.ProjectOid is Guid pid) q = q.Where(b => b.Employee!.ProjectOid == pid);
        if (req.SubcontractorId is Guid sid) q = q.Where(b => b.Employee!.SubcontractorId == sid);

        var list = await q.OrderByDescending(b => b.CreatedAt).Take(10_000).ToListAsync(ct);
        var headers = new[] { "Дата", "ФИО", "Субподрядчик", "Объект", "Действие", "Причина", "Инициатор" };
        var rows = list.Select(b => (IReadOnlyList<string>)[
            b.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
            b.Employee!.FullName,
            b.Employee.Subcontractor?.Name ?? "—",
            b.Employee.Project?.Name ?? "—",
            b.ActionType == EmployeeBlockActionType.Block ? "Блокировка" : "Разблокировка",
            b.Reason,
            b.InitiatedBy?.FullName ?? "—"
        ]).ToList();
        return req.Format.Trim().ToLowerInvariant() == "pdf"
            ? PdfReportWriter.Build("Блокировки сотрудников", "blokirovki", headers, rows)
            : CsvReportWriter.Build("blokirovki", headers, rows);
    }
}

public sealed class ExportDocumentRequestsHandler(
    ITansuDbContext db,
    ITansuAccessService accessService,
    ICurrentUser currentUser) : IRequestHandler<ExportDocumentRequestsQuery, ExportFileDto>
{
    public async Task<ExportFileDto> Handle(ExportDocumentRequestsQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanViewReports, "Экспорт недоступен.");

        var q = db.DocumentRequests.AsNoTracking()
            .Include(r => r.Subcontractor)
            .Include(r => r.Project)
            .AsQueryable();
        if (currentUser.SubcontractorId is Guid subId)
            q = q.Where(r => r.SubcontractorId == subId);
        if (!string.IsNullOrWhiteSpace(req.RequestType)) q = q.Where(r => r.RequestType == req.RequestType);
        if (req.From is DateTimeOffset from) q = q.Where(r => r.CreatedAt >= from);
        if (req.To is DateTimeOffset to) q = q.Where(r => r.CreatedAt <= to);

        var list = await q.OrderByDescending(r => r.CreatedAt).Take(10_000).ToListAsync(ct);
        var ids = list.Select(r => r.Id).ToList();
        var sheets = ids.Count == 0
            ? []
            : await db.DocumentApprovalSheet.AsNoTracking()
                .Where(a => ids.Contains(a.DocumentRequestId))
                .ToListAsync(ct);
        var sheetsByRequest = sheets.GroupBy(s => s.DocumentRequestId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Domain.Entities.DocumentApprovalSheetEntry>)g.ToList());

        if (!string.IsNullOrWhiteSpace(req.Status))
        {
            var statusFilter = req.Status.Trim();
            list = list.Where(r =>
            {
                sheetsByRequest.TryGetValue(r.Id, out var requestSheets);
                requestSheets ??= [];
                var (status, _, _, _) = DocumentRequestStatusResolver.Resolve(requestSheets);
                return string.Equals(status, statusFilter, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        var headers = new[] { "Дата", "Тип", "Название", "Статус", "Субподрядчик", "Объект" };
        var rows = list.Select(r =>
        {
            sheetsByRequest.TryGetValue(r.Id, out var requestSheets);
            requestSheets ??= [];
            var (status, _, _, _) = DocumentRequestStatusResolver.Resolve(requestSheets);
            return (IReadOnlyList<string>)[
                r.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                r.RequestType,
                r.Title,
                status ?? "—",
                r.Subcontractor?.Name ?? "—",
                r.Project?.Name ?? "—"
            ];
        }).ToList();
        return req.Format.Trim().ToLowerInvariant() == "pdf"
            ? PdfReportWriter.Build("Заявки субподрядчиков", "zayavki", headers, rows)
            : CsvReportWriter.Build("zayavki", headers, rows);
    }
}

public sealed class ExportExpiringDocumentsHandler(
    ITansuDbContext db,
    ITansuAccessService accessService,
    ICurrentUser currentUser) : IRequestHandler<ExportExpiringDocumentsQuery, ExportFileDto>
{
    public async Task<ExportFileDto> Handle(ExportExpiringDocumentsQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanViewReports, "Экспорт недоступен.");

        var until = DateTimeOffset.UtcNow.AddDays(Math.Clamp(req.DaysAhead, 1, 90));
        var q = db.EmployeeDocuments.AsNoTracking()
            .Include(d => d.Employee!).ThenInclude(e => e!.Subcontractor)
            .Where(d => d.ExpiresAt != null && d.ExpiresAt <= until && d.ExpiresAt >= DateTimeOffset.UtcNow);
        if (currentUser.SubcontractorId is Guid subId)
            q = q.Where(d => d.Employee!.SubcontractorId == subId);

        var list = await q.OrderBy(d => d.ExpiresAt).Take(10_000).ToListAsync(ct);
        var headers = new[] { "ФИО", "Субподрядчик", "Тип документа", "Истекает" };
        var rows = list.Select(d => (IReadOnlyList<string>)[
            d.Employee!.FullName,
            d.Employee.Subcontractor?.Name ?? "—",
            d.DocumentType,
            d.ExpiresAt!.Value.ToLocalTime().ToString("dd.MM.yyyy")
        ]).ToList();
        return req.Format.Trim().ToLowerInvariant() == "pdf"
            ? PdfReportWriter.Build("Истекающие документы", "istekayushchie-dokumenty", headers, rows)
            : CsvReportWriter.Build("istekayushchie-dokumenty", headers, rows);
    }
}

public sealed class GetSubcontractorComplianceHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<GetSubcontractorComplianceQuery, IReadOnlyList<SubcontractorComplianceDto>>
{
    public async Task<IReadOnlyList<SubcontractorComplianceDto>> Handle(GetSubcontractorComplianceQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanViewReports, "Отчёт недоступен.");

        var subsQ = db.Subcontractors.AsNoTracking().Where(s => s.IsActive);
        if (req.SubcontractorId is Guid sid) subsQ = subsQ.Where(s => s.Id == sid);
        var subs = await subsQ.OrderBy(s => s.Name).ToListAsync(ct);
        var subIds = subs.Select(s => s.Id).ToList();
        var employees = await db.Employees.AsNoTracking().Where(e => subIds.Contains(e.SubcontractorId)).ToListAsync(ct);
        var empIds = employees.Select(e => e.Id).ToList();
        var sheets = await db.ApprovalSheet.AsNoTracking().Where(a => empIds.Contains(a.EmployeeId)).ToListAsync(ct);
        var quizzes = await db.EmployeeSafetyQuizCompletions.AsNoTracking()
            .Where(q => empIds.Contains(q.EmployeeId)).Select(q => q.EmployeeId).ToListAsync(ct);
        var quizSet = quizzes.ToHashSet();
        var blocks = await db.EmployeeBlockRecords.AsNoTracking()
            .Where(b => empIds.Contains(b.EmployeeId) && b.ActionType == EmployeeBlockActionType.Block)
            .GroupBy(b => b.EmployeeId).Select(g => g.Key).ToListAsync(ct);
        var blockedSet = blocks.ToHashSet();
        var until = DateTimeOffset.UtcNow.AddDays(14);
        var expiring = await db.EmployeeDocuments.AsNoTracking()
            .Where(d => empIds.Contains(d.EmployeeId) && d.ExpiresAt != null && d.ExpiresAt <= until)
            .GroupBy(d => d.Employee!.SubcontractorId)
            .Select(g => new { SubId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var expiringMap = expiring.ToDictionary(x => x.SubId, x => x.Count);

        return subs.Select(s =>
        {
            var emps = employees.Where(e => e.SubcontractorId == s.Id).ToList();
            var approved = emps.Count(e =>
            {
                var st = Employees.EmployeeStatusResolver.ResolveFromSheets(sheets.Where(sh => sh.EmployeeId == e.Id).ToList());
                return st == ApprovalStatus.Approved && !blockedSet.Contains(e.Id);
            });
            return new SubcontractorComplianceDto(
                s.Id, s.Name, emps.Count, approved,
                emps.Count(e => blockedSet.Contains(e.Id)),
                emps.Count(e => quizSet.Contains(e.Id)),
                expiringMap.GetValueOrDefault(s.Id));
        }).ToList();
    }
}

public sealed record ExportSiteVisitsReportQuery(
    string Format,
    string? Search,
    Guid? SubcontractorId,
    Guid? ProjectOid,
    DateTimeOffset? From,
    DateTimeOffset? To) : IRequest<ExportFileDto>;

public sealed class ExportSiteVisitsReportHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<ExportSiteVisitsReportQuery, ExportFileDto>
{
    public async Task<ExportFileDto> Handle(ExportSiteVisitsReportQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanViewReports, "Экспорт недоступен для вашей роли.");

        var file = await SiteVisitJournal.SiteVisitJournalExportBuilder.BuildAsync(
            db, access, req.Format, req.Search, req.SubcontractorId, req.ProjectOid, req.From, req.To, ct);
        return new ExportFileDto(file.Content, file.ContentType, file.FileName);
    }
}
