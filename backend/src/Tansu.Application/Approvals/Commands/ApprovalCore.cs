using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Approvals.Commands;

internal static class ApprovalCore
{
    /// <summary>
    /// Загружает текущую активную запись согласования, проверяя что это
    /// текущий шаг (нет более ранних pending) и текущий пользователь — согласующий.
    /// Возвращает также Employee (с Subcontractor) и инициатора (User у которого UserType=Subcontractor).
    /// </summary>
    public static async Task<(ApprovalSheetEntry sheet, Employee employee, User initiator)>
        LoadCurrentStepAsync(
            ITansuDbContext db,
            Guid sheetId,
            ICurrentUser currentUser,
            CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException();

        var sheet = await db.ApprovalSheet
            .FirstOrDefaultAsync(a => a.Id == sheetId, ct)
            ?? throw new NotFoundException("ApprovalSheet", sheetId);

        if (sheet.ApproverUserId != userId)
            throw new ForbiddenException("Эта запись согласования назначена другому пользователю.");

        if (sheet.Status != ApprovalStatus.Pending)
            throw new ConflictException("not_pending",
                $"Запись уже имеет статус '{sheet.Status}'.");

        var earlierPending = await db.ApprovalSheet.AnyAsync(a =>
            a.EmployeeId == sheet.EmployeeId &&
            a.RoundId == sheet.RoundId &&
            a.Status == ApprovalStatus.Pending &&
            a.OrderNo < sheet.OrderNo, ct);

        if (earlierPending)
            throw new ConflictException("not_current_step",
                "Дождитесь решения предыдущего согласующего.");

        var employee = await db.Employees
            .Include(e => e.Subcontractor)
            .FirstAsync(e => e.Id == sheet.EmployeeId, ct);

        var initiator = await db.Users
            .Where(u => u.SubcontractorId == employee.SubcontractorId &&
                        u.UserType == UserType.Subcontractor &&
                        u.IsActive)
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (initiator is null)
        {
            initiator = new User
            {
                Id = Guid.Empty,
                FullName = "—",
                Email = "no-reply@tansu.local"
            };
        }

        return (sheet, employee, initiator);
    }
}
