using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.PpeIssuance;

internal static class PpeIssuanceAuthorization
{
    public static async Task EnsureEmployeeAccessAsync(
        Guid employeeId,
        ICurrentUser currentUser,
        ITansuDbContext db,
        CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw new NotFoundException("Employee", employeeId);

        if (currentUser.UserType == UserType.Employee &&
            currentUser.EmployeeId != employeeId)
        {
            throw new ForbiddenException();
        }

        if (currentUser.UserType == UserType.Subcontractor &&
            currentUser.SubcontractorId != employee.SubcontractorId)
        {
            throw new ForbiddenException("Сотрудник принадлежит другому субподрядчику.");
        }
    }
}
