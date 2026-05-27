using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Application.AccessPasses.Commands;

public sealed record RecordEmployeeSiteVisitCommand(string Token, double FaceConfidence)
    : IRequest<EmployeeSiteVisitDto?>;

public sealed class RecordEmployeeSiteVisitHandler(ITansuDbContext db)
    : IRequestHandler<RecordEmployeeSiteVisitCommand, EmployeeSiteVisitDto?>
{
    public async Task<EmployeeSiteVisitDto?> Handle(RecordEmployeeSiteVisitCommand req, CancellationToken ct)
    {
        var token = req.Token.Trim();
        if (string.IsNullOrEmpty(token))
            return null;

        var pass = await db.EmployeeAccessPasses
            .Include(p => p.Employee!)
            .ThenInclude(e => e!.Project)
            .FirstOrDefaultAsync(p => p.Token == token && p.RevokedAt == null, ct);

        if (pass?.Employee is not { } employee)
            return null;

        var visit = new EmployeeSiteVisit
        {
            EmployeeId = employee.Id,
            AccessPassId = pass.Id,
            CheckedInAt = DateTimeOffset.UtcNow,
            FaceConfidence = req.FaceConfidence,
            VerificationMethod = "face_id"
        };

        db.EmployeeSiteVisits.Add(visit);
        await db.SaveChangesAsync(ct);

        return new EmployeeSiteVisitDto(
            visit.Id,
            visit.EmployeeId,
            employee.FullName,
            employee.Project?.Name,
            visit.CheckedInAt,
            visit.FaceConfidence,
            visit.VerificationMethod);
    }
}
