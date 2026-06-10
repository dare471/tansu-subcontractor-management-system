using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Audit.Queries;

public sealed record AuditEventDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid? ActorUserId,
    string? ActorEmail,
    string ActorType,
    string Action,
    string EntityType,
    Guid EntityId,
    Guid? ProjectOid,
    Guid? SubcontractorId,
    string Summary,
    string? PayloadJson);

public sealed record AuditEventsPageDto(IReadOnlyList<AuditEventDto> Items, int Total, int Page, int PageSize);

public sealed record ListAuditEventsQuery(
    int Page = 1,
    int PageSize = 50,
    string? Action = null,
    string? EntityType = null,
    Guid? EntityId = null,
    Guid? ActorUserId = null,
    Guid? ProjectOid = null,
    Guid? SubcontractorId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<AuditEventsPageDto>;

public sealed class ListAuditEventsHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<ListAuditEventsQuery, AuditEventsPageDto>
{
    public async Task<AuditEventsPageDto> Handle(ListAuditEventsQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanViewAuditLog, "Журнал действий недоступен для вашей роли.");

        var page = Math.Max(1, req.Page);
        var pageSize = Math.Clamp(req.PageSize, 1, 200);

        var q = db.AuditEvents.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Action))
            q = q.Where(e => e.Action == req.Action);
        if (!string.IsNullOrWhiteSpace(req.EntityType))
            q = q.Where(e => e.EntityType == req.EntityType);
        if (req.EntityId is Guid eid)
            q = q.Where(e => e.EntityId == eid);
        if (req.ActorUserId is Guid aid)
            q = q.Where(e => e.ActorUserId == aid);
        if (req.ProjectOid is Guid pid)
            q = q.Where(e => e.ProjectOid == pid);
        if (req.SubcontractorId is Guid sid)
            q = q.Where(e => e.SubcontractorId == sid);
        if (req.From is DateTimeOffset from)
            q = q.Where(e => e.OccurredAt >= from);
        if (req.To is DateTimeOffset to)
            q = q.Where(e => e.OccurredAt <= to);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AuditEventDto(
                e.Id, e.OccurredAt, e.ActorUserId, e.ActorEmail, e.ActorType,
                e.Action, e.EntityType, e.EntityId, e.ProjectOid, e.SubcontractorId,
                e.Summary, e.PayloadJson))
            .ToListAsync(ct);

        return new AuditEventsPageDto(items, total, page, pageSize);
    }
}
