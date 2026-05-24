using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests;

internal static class DocumentRequestApprovalCore
{
    public static async Task<(DocumentApprovalSheetEntry sheet, DocumentRequest request, User initiator)>
        LoadCurrentStepAsync(
            ITansuDbContext db,
            Guid sheetId,
            ICurrentUser currentUser,
            CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var sheet = await db.DocumentApprovalSheet
            .FirstOrDefaultAsync(a => a.Id == sheetId, ct)
            ?? throw new NotFoundException("DocumentApprovalSheet", sheetId);

        if (sheet.ApproverUserId != userId)
            throw new ForbiddenException("Эта заявка назначена другому согласующему.");

        if (sheet.Status != ApprovalStatus.Pending)
            throw new ConflictException("not_pending", $"Запись уже имеет статус '{sheet.Status}'.");

        var earlierPending = await db.DocumentApprovalSheet.AnyAsync(a =>
            a.DocumentRequestId == sheet.DocumentRequestId &&
            a.RoundId == sheet.RoundId &&
            a.Status == ApprovalStatus.Pending &&
            a.OrderNo < sheet.OrderNo, ct);

        if (earlierPending)
            throw new ConflictException("not_current_step",
                "Сначала должны быть согласованы предыдущие шаги.");

        var request = await db.DocumentRequests
            .Include(r => r.Subcontractor)
            .Include(r => r.Project)
            .FirstAsync(r => r.Id == sheet.DocumentRequestId, ct);

        var initiator = await db.Users.FirstAsync(u => u.Id == request.CreatedByUserId, ct);

        return (sheet, request, initiator);
    }

    public static async Task<IReadOnlyDictionary<string, User>> ResolveRoleApproversAsync(
        ITansuDbContext db, IEnumerable<string> roles, CancellationToken ct)
    {
        var roleSet = roles.ToHashSet();
        var users = await db.Users
            .Where(u => u.UserType == UserType.Tansu &&
                        u.IsActive &&
                        u.ApproverRole != null &&
                        roleSet.Contains(u.ApproverRole))
            .ToListAsync(ct);

        var map = new Dictionary<string, User>();
        foreach (var role in roleSet)
        {
            var user = users.FirstOrDefault(u => u.ApproverRole == role);
            if (user is null)
            {
                throw new ValidationFailedException(
                    $"Не назначен активный согласующий с ролью «{DocumentRequestLabels.ApproverRole(role)}».");
            }

            map[role] = user;
        }

        return map;
    }
}
