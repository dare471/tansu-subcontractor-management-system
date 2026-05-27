using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeePortal.Commands;

public sealed record ProvisionEmployeePortalCommand(Guid EmployeeId) : IRequest<Unit>;

public sealed class ProvisionEmployeePortalHandler(
    ITansuDbContext db,
    IPasswordHasher hasher,
    IEmployeePortalCredentialWriter credentialWriter) : IRequestHandler<ProvisionEmployeePortalCommand, Unit>
{
    public async Task<Unit> Handle(ProvisionEmployeePortalCommand req, CancellationToken ct)
    {
        var employee = await db.Employees
            .Include(e => e.Subcontractor)
            .FirstOrDefaultAsync(e => e.Id == req.EmployeeId, ct);
        if (employee is null)
            return Unit.Value;

        var otp = GenerateOneTimePassword();
        var portalUser = await db.Users.FirstOrDefaultAsync(u => u.EmployeeId == req.EmployeeId, ct);
        if (portalUser is null)
        {
            portalUser = new User
            {
                FullName = employee.FullName,
                Position = employee.Position,
                Email = BuildPortalEmail(employee.Id),
                UserType = UserType.Employee,
                EmployeeId = employee.Id,
                SubcontractorId = employee.SubcontractorId,
                MustChangePassword = true,
                IsActive = true
            };
            db.Users.Add(portalUser);
        }
        else
        {
            portalUser.FullName = employee.FullName;
            portalUser.Position = employee.Position;
            portalUser.IsActive = true;
            portalUser.MustChangePassword = true;
        }

        portalUser.PasswordHash = hasher.Hash(otp);
        await db.SaveChangesAsync(ct);

        await credentialWriter.WriteAsync(employee.Id, employee.FullName, employee.Iin, otp, ct);
        return Unit.Value;
    }

    internal static string BuildPortalEmail(Guid employeeId) =>
        $"{employeeId:N}@portal.tansu.local";

    private static string GenerateOneTimePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 8)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }
}

public sealed record DeactivateEmployeePortalCommand(Guid EmployeeId) : IRequest<Unit>;

public sealed class DeactivateEmployeePortalHandler(ITansuDbContext db)
    : IRequestHandler<DeactivateEmployeePortalCommand, Unit>
{
    public async Task<Unit> Handle(DeactivateEmployeePortalCommand req, CancellationToken ct)
    {
        var portalUser = await db.Users
            .FirstOrDefaultAsync(u => u.EmployeeId == req.EmployeeId && u.UserType == UserType.Employee, ct);
        if (portalUser is null)
            return Unit.Value;

        portalUser.IsActive = false;
        portalUser.MustChangePassword = true;

        var quiz = await db.EmployeeSafetyQuizCompletions
            .FirstOrDefaultAsync(q => q.EmployeeId == req.EmployeeId, ct);
        if (quiz is not null)
            db.EmployeeSafetyQuizCompletions.Remove(quiz);

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
