using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;
using Tansu.Application.Approvals;

namespace Tansu.Application.Approvals.Queries;

public sealed record GetEmployeeApprovalsQuery(Guid EmployeeId) : IRequest<EmployeeApprovalsDto>;

public sealed class GetEmployeeApprovalsHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetEmployeeApprovalsQuery, EmployeeApprovalsDto>
{
    public async Task<EmployeeApprovalsDto> Handle(GetEmployeeApprovalsQuery req, CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == req.EmployeeId, ct)
            ?? throw new NotFoundException("Employee", req.EmployeeId);

        if (currentUser.UserType == UserType.Subcontractor &&
            currentUser.SubcontractorId != employee.SubcontractorId)
        {
            throw new ForbiddenException();
        }

        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => a.EmployeeId == req.EmployeeId)
            .Include(a => a.Approver)
            .ToListAsync(ct);

        var rounds = sheets
            .GroupBy(a => a.RoundId)
            .OrderBy(g => g.Min(a => a.CreatedAt))
            .Select(g =>
            {
                var steps = g
                    .OrderBy(a => a.OrderNo)
                    .Select(a => new ApprovalHistoryRowDto(
                        a.Id, a.RoundId, a.OrderNo, a.ApproverUserId,
                        a.Approver?.FullName ?? "—",
                        a.Status, a.Comment, a.DecidedAt, a.CreatedAt))
                    .ToList();

                var overall = ApprovalStatusCalculator.DetermineRoundStatus(steps.Select(s => s.Status));
                return new ApprovalRoundSummaryDto(g.Key, overall, steps);
            })
            .ToList();

        var current = rounds.LastOrDefault()?.OverallStatus ?? "draft";
        return new EmployeeApprovalsDto(req.EmployeeId, current, rounds);
    }
}
