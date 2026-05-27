using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Approvals;
using Tansu.Application.Approvals.Queries;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;

namespace Tansu.Application.EmployeePortal.Queries;

public sealed record GetEmployeePortalApprovalsQuery : IRequest<EmployeeApprovalsDto>;

public sealed class GetEmployeePortalApprovalsHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IMediator mediator)
    : IRequestHandler<GetEmployeePortalApprovalsQuery, EmployeeApprovalsDto>
{
    public async Task<EmployeeApprovalsDto> Handle(GetEmployeePortalApprovalsQuery req, CancellationToken ct)
    {
        var employee = await GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);
        return await mediator.Send(new GetEmployeeApprovalsQuery(employee.Id), ct);
    }
}

public sealed record GetEmployeePortalSiteVisitsQuery : IRequest<EmployeePortalSiteVisitsDto>;

public sealed class GetEmployeePortalSiteVisitsHandler(
    ITansuDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<GetEmployeePortalSiteVisitsQuery, EmployeePortalSiteVisitsDto>
{
    public async Task<EmployeePortalSiteVisitsDto> Handle(GetEmployeePortalSiteVisitsQuery req, CancellationToken ct)
    {
        var employee = await GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);

        var visits = await db.EmployeeSiteVisits.AsNoTracking()
            .Where(v => v.EmployeeId == employee.Id)
            .Include(v => v.Employee!)
            .ThenInclude(e => e!.Project)
            .OrderByDescending(v => v.CheckedInAt)
            .Select(v => new EmployeePortalSiteVisitItemDto(
                v.Id,
                v.Employee!.Project != null ? v.Employee.Project.Name : null,
                v.CheckedInAt,
                v.FaceConfidence,
                v.VerificationMethod))
            .ToListAsync(ct);

        return new EmployeePortalSiteVisitsDto(
            visits,
            visits.FirstOrDefault()?.CheckedInAt,
            visits.Count);
    }
}

public sealed record GetEmployeePortalProfileQuery : IRequest<EmployeePortalProfileDto>;

public sealed class GetEmployeePortalProfileHandler(
    ITansuDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<GetEmployeePortalProfileQuery, EmployeePortalProfileDto>
{
    public async Task<EmployeePortalProfileDto> Handle(GetEmployeePortalProfileQuery req, CancellationToken ct)
    {
        var employee = await GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);

        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => a.EmployeeId == employee.Id)
            .ToListAsync(ct);
        var approvalStatus = EmployeeStatusResolver.ResolveFromSheets(sheets);

        var pass = await db.EmployeeAccessPasses.AsNoTracking()
            .Where(p => p.EmployeeId == employee.Id && p.RevokedAt == null)
            .OrderByDescending(p => p.IssuedAt)
            .FirstOrDefaultAsync(ct);

        return new EmployeePortalProfileDto(
            employee.Id,
            employee.FullName,
            employee.Position,
            employee.Phone,
            employee.Iin,
            employee.Subcontractor?.Name ?? "—",
            employee.Project?.Name,
            approvalStatus,
            !string.IsNullOrEmpty(employee.PhotoPath),
            pass?.IssuedAt);
    }
}
