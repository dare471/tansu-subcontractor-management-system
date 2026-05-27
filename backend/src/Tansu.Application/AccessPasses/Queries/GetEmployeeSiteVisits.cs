using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.AccessPasses.Queries;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.AccessPasses.Queries;

public sealed record GetEmployeeSiteVisitsQuery(Guid EmployeeId) : IRequest<IReadOnlyList<EmployeeSiteVisitDto>>;

public sealed class GetEmployeeSiteVisitsHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetEmployeeSiteVisitsQuery, IReadOnlyList<EmployeeSiteVisitDto>>
{
    public async Task<IReadOnlyList<EmployeeSiteVisitDto>> Handle(GetEmployeeSiteVisitsQuery req, CancellationToken ct)
    {
        await AccessPassAuthorization.EnsureEmployeeAccessAsync(req.EmployeeId, currentUser, db, ct);

        return await db.EmployeeSiteVisits.AsNoTracking()
            .Where(v => v.EmployeeId == req.EmployeeId)
            .Include(v => v.Employee!)
            .ThenInclude(e => e!.Project)
            .OrderByDescending(v => v.CheckedInAt)
            .Select(v => new EmployeeSiteVisitDto(
                v.Id,
                v.EmployeeId,
                v.Employee!.FullName,
                v.Employee.Project != null ? v.Employee.Project.Name : null,
                v.CheckedInAt,
                v.FaceConfidence,
                v.VerificationMethod))
            .ToListAsync(ct);
    }
}
