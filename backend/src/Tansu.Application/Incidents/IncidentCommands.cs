using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeeDocuments.Commands;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Incidents;

public sealed record SiteIncidentDto(
    Guid Id,
    Guid ProjectOid,
    string? ProjectName,
    DateTimeOffset OccurredAt,
    string Title,
    string Description,
    string Severity,
    string Status,
    Guid? SubcontractorId,
    string? SubcontractorName,
    bool BlockUntilResolved,
    string? ResolutionNotes,
    DateTimeOffset? ResolvedAt,
    IReadOnlyList<Guid> EmployeeIds);

public sealed record CreateSiteIncidentCommand(
    Guid ProjectOid,
    DateTimeOffset OccurredAt,
    string Title,
    string Description,
    string Severity,
    Guid? SubcontractorId,
    bool BlockUntilResolved,
    IReadOnlyList<Guid> EmployeeIds) : IRequest<SiteIncidentDto>;

public sealed record UpdateSiteIncidentStatusCommand(
    Guid Id,
    string Status,
    string? ResolutionNotes) : IRequest<SiteIncidentDto>;

public sealed record ListSiteIncidentsQuery(
    Guid? ProjectOid = null,
    Guid? SubcontractorId = null,
    string? Status = null) : IRequest<IReadOnlyList<SiteIncidentDto>>;

public sealed class CreateSiteIncidentHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IMediator mediator,
    IAuditRecorder audit) : IRequestHandler<CreateSiteIncidentCommand, SiteIncidentDto>
{
    public async Task<SiteIncidentDto> Handle(CreateSiteIncidentCommand req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanBlockEmployees, "Создание инцидентов недоступно для вашей роли.");

        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        if (!await db.ProjectRefs.AnyAsync(p => p.ProjectOid == req.ProjectOid, ct))
            throw new ValidationFailedException("Проект не найден.");

        var incident = new SiteIncident
        {
            ProjectOid = req.ProjectOid,
            OccurredAt = req.OccurredAt,
            ReportedByUserId = userId,
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            Severity = req.Severity,
            SubcontractorId = req.SubcontractorId,
            BlockUntilResolved = req.BlockUntilResolved
        };
        db.SiteIncidents.Add(incident);
        foreach (var empId in req.EmployeeIds.Distinct())
            db.SiteIncidentEmployees.Add(new SiteIncidentEmployee { IncidentId = incident.Id, EmployeeId = empId });

        audit.Record(new AuditEntry(
            AuditActions.IncidentCreated, "site_incident", incident.Id,
            $"Инцидент: {incident.Title}", ProjectOid: req.ProjectOid, SubcontractorId: req.SubcontractorId));
        await db.SaveChangesAsync(ct);

        if (req.BlockUntilResolved)
        {
            foreach (var empId in req.EmployeeIds.Distinct())
            {
                try
                {
                    await mediator.Send(new BlockEmployeeCommand(empId, $"Инцидент: {incident.Title}"), ct);
                }
                catch (ConflictException) { }
            }
        }

        return await MapAsync(db, incident.Id, ct);
    }

    internal static async Task<SiteIncidentDto> MapAsync(ITansuDbContext db, Guid id, CancellationToken ct)
    {
        var i = await db.SiteIncidents.AsNoTracking()
            .Include(x => x.Project)
            .Include(x => x.Subcontractor)
            .Include(x => x.LinkedEmployees)
            .FirstAsync(x => x.Id == id, ct);
        return new SiteIncidentDto(
            i.Id, i.ProjectOid, i.Project?.Name, i.OccurredAt, i.Title, i.Description,
            i.Severity, i.Status, i.SubcontractorId, i.Subcontractor?.Name,
            i.BlockUntilResolved, i.ResolutionNotes, i.ResolvedAt,
            i.LinkedEmployees.Select(e => e.EmployeeId).ToList());
    }
}

public sealed class UpdateSiteIncidentStatusHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IAuditRecorder audit) : IRequestHandler<UpdateSiteIncidentStatusCommand, SiteIncidentDto>
{
    public async Task<SiteIncidentDto> Handle(UpdateSiteIncidentStatusCommand req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanBlockEmployees, "Обновление инцидентов недоступно.");

        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var incident = await db.SiteIncidents.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("SiteIncident", req.Id);
        incident.Status = req.Status;
        if (!string.IsNullOrWhiteSpace(req.ResolutionNotes))
            incident.ResolutionNotes = req.ResolutionNotes.Trim();
        if (req.Status is "resolved" or "closed")
        {
            incident.ResolvedAt = DateTimeOffset.UtcNow;
            incident.ResolvedByUserId = userId;
            audit.Record(new AuditEntry(AuditActions.IncidentResolved, "site_incident", incident.Id, "Инцидент закрыт",
                ProjectOid: incident.ProjectOid));
        }
        else
        {
            audit.Record(new AuditEntry(AuditActions.IncidentUpdated, "site_incident", incident.Id,
                $"Статус: {req.Status}", ProjectOid: incident.ProjectOid));
        }
        await db.SaveChangesAsync(ct);
        return await CreateSiteIncidentHandler.MapAsync(db, incident.Id, ct);
    }
}

public sealed class ListSiteIncidentsHandler(
    ITansuDbContext db,
    ITansuAccessService accessService,
    ICurrentUser currentUser) : IRequestHandler<ListSiteIncidentsQuery, IReadOnlyList<SiteIncidentDto>>
{
    public async Task<IReadOnlyList<SiteIncidentDto>> Handle(ListSiteIncidentsQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        var q = db.SiteIncidents.AsNoTracking()
            .Include(x => x.Project)
            .Include(x => x.Subcontractor)
            .Include(x => x.LinkedEmployees)
            .AsQueryable();

        if (currentUser.UserType == UserType.Subcontractor && currentUser.SubcontractorId is Guid subId)
            q = q.Where(x => x.SubcontractorId == subId);
        else
            accessService.EnsurePermission(access, p => p.CanViewEmployees, "Список инцидентов недоступен.");

        if (req.ProjectOid is Guid pid) q = q.Where(x => x.ProjectOid == pid);
        if (req.SubcontractorId is Guid sid) q = q.Where(x => x.SubcontractorId == sid);
        if (!string.IsNullOrWhiteSpace(req.Status)) q = q.Where(x => x.Status == req.Status);

        var list = await q.OrderByDescending(x => x.OccurredAt).Take(500).ToListAsync(ct);
        return list.Select(i => new SiteIncidentDto(
            i.Id, i.ProjectOid, i.Project?.Name, i.OccurredAt, i.Title, i.Description,
            i.Severity, i.Status, i.SubcontractorId, i.Subcontractor?.Name,
            i.BlockUntilResolved, i.ResolutionNotes, i.ResolvedAt,
            i.LinkedEmployees.Select(e => e.EmployeeId).ToList())).ToList();
    }
}
