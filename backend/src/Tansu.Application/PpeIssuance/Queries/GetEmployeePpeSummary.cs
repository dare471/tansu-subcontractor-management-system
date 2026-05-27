using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeePortal.Queries;

namespace Tansu.Application.PpeIssuance.Queries;

public sealed record GetEmployeePpeSummaryQuery(Guid EmployeeId) : IRequest<EmployeePpeSummaryDto>;

public sealed class GetEmployeePpeSummaryHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetEmployeePpeSummaryQuery, EmployeePpeSummaryDto>
{
    public async Task<EmployeePpeSummaryDto> Handle(GetEmployeePpeSummaryQuery req, CancellationToken ct)
    {
        await PpeIssuanceAuthorization.EnsureEmployeeAccessAsync(req.EmployeeId, currentUser, db, ct);

        var rows = await db.EmployeePpeIssuances.AsNoTracking()
            .Where(p => p.EmployeeId == req.EmployeeId)
            .Include(p => p.IssuedBy)
            .OrderByDescending(p => p.IssuedAt)
            .ToListAsync(ct);

        return PpeIssuanceMapper.ToSummary(rows);
    }
}

public sealed record GetEmployeePortalPpeQuery : IRequest<EmployeePpeSummaryDto>;

public sealed class GetEmployeePortalPpeHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetEmployeePortalPpeQuery, EmployeePpeSummaryDto>
{
    public async Task<EmployeePpeSummaryDto> Handle(GetEmployeePortalPpeQuery req, CancellationToken ct)
    {
        var employee = await GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);
        var rows = await db.EmployeePpeIssuances.AsNoTracking()
            .Where(p => p.EmployeeId == employee.Id)
            .Include(p => p.IssuedBy)
            .OrderByDescending(p => p.IssuedAt)
            .ToListAsync(ct);

        return PpeIssuanceMapper.ToSummary(rows);
    }
}
