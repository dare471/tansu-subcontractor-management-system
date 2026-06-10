using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.SiteVisitJournal;

public sealed record SiteVisitJournalItemDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeFullName,
    string EmployeePosition,
    string SubcontractorName,
    string? ProjectName,
    string? TerminalLocation,
    DateTimeOffset CheckedInAt,
    DateTimeOffset? CheckedOutAt,
    string DataSource,
    string DataSourceLabel,
    double? FaceConfidence,
    string VerificationMethod);

public sealed record SiteVisitJournalPageDto(
    IReadOnlyList<SiteVisitJournalItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ListSiteVisitJournalQuery(
    int Page = 1,
    int PageSize = 50,
    string? Search = null,
    Guid? SubcontractorId = null,
    Guid? ProjectOid = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<SiteVisitJournalPageDto>;

public sealed class ListSiteVisitJournalHandler(
    ITansuDbContext db,
    ITansuAccessService accessService,
    ICurrentUser currentUser) : IRequestHandler<ListSiteVisitJournalQuery, SiteVisitJournalPageDto>
{
    public async Task<SiteVisitJournalPageDto> Handle(ListSiteVisitJournalQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanViewVisitJournal, "Журнал посещений недоступен для вашей роли.");

        var subcontractorId = req.SubcontractorId;
        if (currentUser.UserType == UserType.Subcontractor)
            subcontractorId = currentUser.SubcontractorId
                ?? throw new Common.Exceptions.ForbiddenException("Контекст субподрядчика отсутствует.");

        var page = req.Page < 1 ? 1 : req.Page;
        var pageSize = req.PageSize switch
        {
            < 1 => 50,
            > 200 => 200,
            _ => req.PageSize
        };

        var q = SiteVisitJournalQueryBuilder.ApplyFilters(
            db.EmployeeSiteVisits.AsNoTracking(),
            access,
            req.Search,
            subcontractorId,
            req.ProjectOid,
            req.From,
            req.To);

        var total = await q.CountAsync(ct);

        var items = await q
            .Include(v => v.Employee!)
            .ThenInclude(e => e!.Subcontractor)
            .Include(v => v.Employee!)
            .ThenInclude(e => e!.Project)
            .OrderByDescending(v => v.CheckedInAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new SiteVisitJournalItemDto(
                v.Id,
                v.EmployeeId,
                v.Employee!.FullName,
                v.Employee.Position,
                v.Employee.Subcontractor!.Name,
                v.Employee.Project != null ? v.Employee.Project.Name : null,
                v.TerminalLocation,
                v.CheckedInAt,
                v.CheckedOutAt,
                v.DataSource,
                SiteVisitDataSource.Label(v.DataSource),
                v.FaceConfidence,
                v.VerificationMethod))
            .ToListAsync(ct);

        return new SiteVisitJournalPageDto(items, total, page, pageSize);
    }
}

internal static class SiteVisitJournalQueryBuilder
{
    public static IQueryable<Domain.Entities.EmployeeSiteVisit> ApplyFilters(
        IQueryable<Domain.Entities.EmployeeSiteVisit> q,
        TansuAccessContext access,
        string? search,
        Guid? subcontractorId,
        Guid? projectOid,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (access.VisibleSubcontractorIds is { } scopeSubs)
            q = q.Where(v => scopeSubs.Contains(v.Employee!.SubcontractorId));

        if (access.VisibleProjectOids is { } scopeProjects)
            q = q.Where(v => scopeProjects.Contains(v.Employee!.ProjectOid));

        if (subcontractorId is { } sid)
        {
            if (access.VisibleSubcontractorIds is { } visible && !visible.Contains(sid))
                throw new Common.Exceptions.ForbiddenException("Субподрядчик вне вашей области видимости.");
            q = q.Where(v => v.Employee!.SubcontractorId == sid);
        }

        if (projectOid is { } poid)
        {
            if (access.VisibleProjectOids is { } projects && !projects.Contains(poid))
                throw new Common.Exceptions.ForbiddenException("Проект вне вашей области видимости.");
            q = q.Where(v => v.Employee!.ProjectOid == poid);
        }

        if (from is { } fromDt)
            q = q.Where(v => v.CheckedInAt >= fromDt);

        if (to is { } toDt)
            q = q.Where(v => v.CheckedInAt <= toDt);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(v =>
                v.Employee!.FullName.ToLower().Contains(s) ||
                v.Employee.Iin.Contains(s));
        }

        return q;
    }
}
