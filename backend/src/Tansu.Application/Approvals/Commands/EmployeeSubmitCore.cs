using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeePhotoReviews.Commands;
using Tansu.Contracts.Messages;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Approvals.Commands;

internal static class EmployeeSubmitCore
{
    internal sealed record PreparedSubmission(
        Employee Employee,
        Guid RoundId,
        List<ApprovalSheetEntry> Sheets,
        User FirstApprover,
        ApprovalSheetEntry FirstSheet);

    internal static async Task<Employee> LoadEmployeeForSubmitAsync(
        ITansuDbContext db,
        Guid employeeId,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var employee = await db.Employees
            .Include(e => e.Subcontractor)
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw new NotFoundException("Employee", employeeId);

        if (currentUser.UserType != UserType.Subcontractor ||
            currentUser.SubcontractorId != employee.SubcontractorId)
        {
            throw new ForbiddenException("Только субподрядчик-владелец может отправить сотрудника на согласование.");
        }

        return employee;
    }

    internal static async Task EnsureSubmittableAsync(
        ITansuDbContext db,
        Employee employee,
        Guid? excludeDraftBatchId,
        CancellationToken ct)
    {
        var anyPending = await db.ApprovalSheet
            .AnyAsync(a => a.EmployeeId == employee.Id && a.Status == ApprovalStatus.Pending, ct);
        if (anyPending)
            throw new ConflictException("approval_in_progress",
                $"Сотрудник «{employee.FullName}» уже находится на согласовании.");

        var draftBatchIds = await db.EmployeeApprovalBatchMembers
            .Where(m => m.EmployeeId == employee.Id)
            .Join(
                db.EmployeeApprovalBatches.Where(b => b.Status == BatchStatus.Draft),
                m => m.BatchId,
                b => b.Id,
                (m, _) => m.BatchId)
            .ToListAsync(ct);

        if (draftBatchIds.Any(id => excludeDraftBatchId == null || id != excludeDraftBatchId))
        {
            throw new ConflictException("employee_in_draft_batch",
                $"Сотрудник «{employee.FullName}» уже включён в черновик пакета.");
        }

        var isBlocked = await EmployeeDocuments.EmployeeBlockHelper.IsBlockedAsync(db, employee.Id, ct);
        if (!isBlocked)
        {
            var sheets = await db.ApprovalSheet.AsNoTracking()
                .Where(a => a.EmployeeId == employee.Id)
                .ToListAsync(ct);
            var status = Employees.EmployeeStatusResolver.ResolveFromSheets(sheets);
            if (status == ApprovalStatus.Approved)
            {
                throw new ConflictException("employee_already_approved",
                    $"Сотрудник «{employee.FullName}» уже согласован. Повторная отправка возможна после блокировки или отклонения.");
            }
        }
    }

    internal static async Task EnsurePhotoApprovedAsync(
        ITansuDbContext db,
        Employee employee,
        IMediator mediator,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(employee.PhotoPath))
        {
            throw new ValidationFailedException(
                $"У сотрудника «{employee.FullName}» не загружено фото. Загрузите фото и дождитесь проверки.");
        }

        if (employee.PhotoReviewStatus is null)
        {
            await mediator.Send(new RunEmployeePhotoAutoReviewCommand(employee.Id), ct);
            var fresh = await db.Employees.AsNoTracking()
                .FirstAsync(e => e.Id == employee.Id, ct);
            employee.PhotoReviewStatus = fresh.PhotoReviewStatus;
            employee.PhotoReviewReason = fresh.PhotoReviewReason;
        }

        switch (employee.PhotoReviewStatus)
        {
            case EmployeePhotoReviewStatus.Approved:
                return;
            case EmployeePhotoReviewStatus.Rejected:
                throw new ValidationFailedException(
                    employee.PhotoReviewReason ??
                    $"Фото сотрудника «{employee.FullName}» не прошло проверку. Загрузите новое фото.");
            case EmployeePhotoReviewStatus.Pending:
                throw new ValidationFailedException(
                    $"Фото сотрудника «{employee.FullName}» ожидает проверки ответственным сотрудником ТАНСУ.");
            default:
                throw new ValidationFailedException(
                    $"Фото сотрудника «{employee.FullName}» не прошло проверку.");
        }
    }

    internal static async Task<PreparedSubmission> PrepareSubmissionAsync(
        ITansuDbContext db,
        Employee employee,
        Guid initiatorId,
        Guid? batchId,
        CancellationToken ct)
    {
        var matrix = await db.ApprovalMatrix
            .Where(m => m.ProjectOid == employee.ProjectOid &&
                        m.SubcontractorId == employee.SubcontractorId)
            .OrderBy(m => m.OrderNo)
            .ToListAsync(ct);

        if (matrix.Count == 0)
            throw new ValidationFailedException(
                "Не настроена матрица согласования для этого проекта/субподрядчика.");

        var approverIds = matrix.Select(m => m.UserId).ToHashSet();
        var approvers = await db.Users
            .Where(u => approverIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var roundId = Guid.NewGuid();
        var sheets = matrix.Select(step => new ApprovalSheetEntry
        {
            EmployeeId = employee.Id,
            ApproverUserId = step.UserId,
            OrderNo = step.OrderNo,
            RoundId = roundId,
            BatchId = batchId,
            Status = ApprovalStatus.Pending
        }).ToList();

        var firstSheet = sheets.OrderBy(s => s.OrderNo).First();
        var firstApprover = approvers[firstSheet.ApproverUserId];

        return new PreparedSubmission(employee, roundId, sheets, firstApprover, firstSheet);
    }

    internal static async Task PublishIndividualNotificationsAsync(
        IPublishEndpoint publisher,
        Employee employee,
        User initiator,
        PreparedSubmission prepared,
        CancellationToken ct)
    {
        await publisher.Publish(new ApprovalSubmittedMessage(
            employee.Id,
            employee.FullName,
            employee.SubcontractorId,
            employee.Subcontractor!.Name,
            employee.ProjectOid,
            initiator.Id,
            initiator.Email,
            prepared.FirstApprover.Id,
            prepared.FirstApprover.Email,
            prepared.FirstApprover.FullName,
            DateTimeOffset.UtcNow), ct);

        await publisher.Publish(new NextApproverNotificationMessage(
            employee.Id,
            employee.FullName,
            employee.SubcontractorId,
            employee.Subcontractor!.Name,
            employee.ProjectOid,
            prepared.FirstApprover.Id,
            prepared.FirstApprover.Email,
            prepared.FirstApprover.FullName,
            prepared.FirstSheet.OrderNo,
            DateTimeOffset.UtcNow), ct);
    }
}
