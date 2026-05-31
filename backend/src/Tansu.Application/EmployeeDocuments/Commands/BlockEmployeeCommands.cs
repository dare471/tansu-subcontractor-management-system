using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.AccessPasses.Commands;
using Tansu.Application.Approvals;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Contracts.Messages;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeeDocuments.Commands;

public sealed record BlockEmployeeCommand(Guid EmployeeId, string Reason) : IRequest<EmployeeBlockRecordDto>;

public sealed class BlockEmployeeHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IMediator mediator,
    IHikAccessService hikAccess,
    IPublishEndpoint publisher) : IRequestHandler<BlockEmployeeCommand, EmployeeBlockRecordDto>
{
    public async Task<EmployeeBlockRecordDto> Handle(BlockEmployeeCommand req, CancellationToken ct)
    {
        await EmployeeBlockAuthorization.EnsureCanInitiateBlockAsync(accessService, ct);
        await accessService.EnsureEmployeeVisibleAsync(req.EmployeeId, ct);

        var employee = await db.Employees
            .Include(e => e.Subcontractor)
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == req.EmployeeId, ct)
            ?? throw new NotFoundException("Employee", req.EmployeeId);

        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => a.EmployeeId == employee.Id)
            .ToListAsync(ct);
        var approvalStatus = EmployeeStatusResolver.ResolveFromSheets(sheets);
        if (approvalStatus != ApprovalStatus.Approved)
        {
            throw new ValidationFailedException(
                "Блокировать можно только ранее согласованных сотрудников.");
        }

        if (await EmployeeBlockHelper.IsBlockedAsync(db, employee.Id, ct))
            throw new ConflictException("employee_already_blocked", "Сотрудник уже заблокирован.");

        var reason = req.Reason.Trim();
        if (reason.Length < 3)
            throw new ValidationFailedException("Укажите причину блокировки (не короче 3 символов).");

        var initiatorId = currentUser.UserId ?? throw new UnauthorizedException();
        var initiator = await db.Users.AsNoTracking().FirstAsync(u => u.Id == initiatorId, ct);
        var initiatorRole = initiator.TansuRole ?? initiator.ApproverRole;

        var record = new EmployeeBlockRecord
        {
            EmployeeId = employee.Id,
            InitiatedByUserId = initiatorId,
            ActionType = EmployeeBlockActionType.Block,
            Reason = reason,
            Status = EmployeeBlockRequestStatus.Applied,
            InitiatorRole = initiatorRole
        };

        db.EmployeeBlockRecords.Add(record);
        await db.SaveChangesAsync(ct);

        await mediator.Send(new RevokeEmployeeAccessPassesCommand(employee.Id), ct);
        await hikAccess.RevokeAccessAsync(employee.Id, reason, ct);

        var notifyEmails = await db.Users.AsNoTracking()
            .Where(u => u.IsActive &&
                        u.UserType == UserType.Subcontractor &&
                        u.SubcontractorId == employee.SubcontractorId)
            .Select(u => u.Email)
            .ToListAsync(ct);

        await publisher.Publish(new EmployeeBlockedMessage(
            employee.Id,
            employee.FullName,
            employee.SubcontractorId,
            employee.Subcontractor!.Name,
            employee.ProjectOid,
            employee.Project?.Name,
            initiatorId,
            initiator.FullName,
            initiatorRole ?? "—",
            reason,
            notifyEmails,
            DateTimeOffset.UtcNow), ct);

        record.InitiatedBy = initiator;
        return EmployeeDocumentMapper.ToBlockDto(record);
    }
}

public sealed record UnblockEmployeeAfterReapprovalCommand(Guid EmployeeId, Guid InitiatorUserId)
    : IRequest<Unit>;

public sealed class UnblockEmployeeAfterReapprovalHandler(
    ITansuDbContext db,
    IHikAccessService hikAccess) : IRequestHandler<UnblockEmployeeAfterReapprovalCommand, Unit>
{
    public async Task<Unit> Handle(UnblockEmployeeAfterReapprovalCommand req, CancellationToken ct)
    {
        if (!await EmployeeBlockHelper.IsBlockedAsync(db, req.EmployeeId, ct))
            return Unit.Value;

        var record = new EmployeeBlockRecord
        {
            EmployeeId = req.EmployeeId,
            InitiatedByUserId = req.InitiatorUserId,
            ActionType = EmployeeBlockActionType.Unblock,
            Reason = "Повторное согласование завершено. Доступ восстановлен.",
            Status = EmployeeBlockRequestStatus.Applied,
            InitiatorRole = null
        };

        db.EmployeeBlockRecords.Add(record);
        await db.SaveChangesAsync(ct);
        await hikAccess.GrantAccessAsync(req.EmployeeId, ct);
        return Unit.Value;
    }
}
