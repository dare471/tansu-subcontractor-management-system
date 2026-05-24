using MassTransit;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Approvals;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Contracts.Messages;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Approvals.Commands;

internal static class EmployeeBatchCore
{
    public static async Task<EmployeeApprovalBatch> LoadOwnedBatchAsync(
        ITansuDbContext db,
        Guid batchId,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var sid = currentUser.SubcontractorId
            ?? throw new ForbiddenException("Контекст субподрядчика отсутствует.");

        var batch = await db.EmployeeApprovalBatches
            .Include(b => b.Project)
            .Include(b => b.Subcontractor)
            .Include(b => b.Members)
            .ThenInclude(m => m.Employee)
            .FirstOrDefaultAsync(b => b.Id == batchId && b.SubcontractorId == sid, ct)
            ?? throw new NotFoundException("EmployeeApprovalBatch", batchId);

        return batch;
    }

    public static async Task EnsureDraftAsync(EmployeeApprovalBatch batch)
    {
        if (batch.Status != BatchStatus.Draft)
            throw new ConflictException("batch_not_draft", "Пакет уже отправлен на согласование.");
        await Task.CompletedTask;
    }

    public static ApprovalBatchDto ToDto(EmployeeApprovalBatch batch, IReadOnlyDictionary<Guid, string?>? statuses = null)
    {
        var employees = batch.Members
            .OrderBy(m => m.AddedAt)
            .Select(m =>
            {
                string? status = null;
                statuses?.TryGetValue(m.EmployeeId, out status);
                return new ApprovalBatchEmployeeDto(
                    m.EmployeeId,
                    m.Employee?.FullName ?? "—",
                    m.Employee?.Position ?? "—",
                    status);
            })
            .ToList();

        return new ApprovalBatchDto(
            batch.Id,
            batch.Title,
            batch.Status,
            batch.ProjectOid,
            batch.Project?.Name,
            batch.EmployeeCount,
            batch.CreatedAt,
            batch.SubmittedAt,
            employees);
    }

    public static async Task PublishBatchNotificationsAsync(
        IPublishEndpoint publisher,
        EmployeeApprovalBatch batch,
        User initiator,
        User firstApprover,
        IReadOnlyList<Employee> employees,
        CancellationToken ct)
    {
        var memberInfos = employees
            .Select(e => new EmployeeBatchMemberInfo(e.Id, e.FullName, e.Position))
            .ToList();

        await publisher.Publish(new EmployeeBatchSubmittedMessage(
            batch.Id,
            batch.Title,
            batch.SubcontractorId,
            batch.Subcontractor?.Name ?? "—",
            batch.ProjectOid,
            batch.Project?.Name,
            initiator.Id,
            initiator.Email,
            firstApprover.Id,
            firstApprover.Email,
            firstApprover.FullName,
            memberInfos,
            DateTimeOffset.UtcNow), ct);

        await publisher.Publish(new NextApproverNotificationMessage(
            employees[0].Id,
            $"{batch.Title} ({employees.Count} чел.)",
            batch.SubcontractorId,
            batch.Subcontractor?.Name ?? "—",
            batch.ProjectOid,
            firstApprover.Id,
            firstApprover.Email,
            firstApprover.FullName,
            1,
            DateTimeOffset.UtcNow), ct);
    }

    internal static async Task<IReadOnlyDictionary<Guid, string?>> LoadMemberStatusesAsync(
        ITansuDbContext db,
        IEnumerable<Guid> employeeIds,
        CancellationToken ct)
    {
        var ids = employeeIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string?>();

        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeId))
            .ToListAsync(ct);

        var result = new Dictionary<Guid, string?>();
        foreach (var id in ids)
        {
            var employeeSheets = sheets.Where(s => s.EmployeeId == id).ToList();
            if (employeeSheets.Count == 0)
            {
                result[id] = null;
                continue;
            }

            var latestRoundId = employeeSheets
                .OrderByDescending(s => s.CreatedAt)
                .First()
                .RoundId;

            var roundSheets = employeeSheets.Where(s => s.RoundId == latestRoundId).ToList();
            var roundStatus = ApprovalStatusCalculator.DetermineRoundStatus(roundSheets.Select(s => s.Status));
            result[id] = roundStatus == "draft" ? null : roundStatus;
        }

        return result;
    }
}
