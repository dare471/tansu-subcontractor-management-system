using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Contracts.Messages;
using Tansu.Domain.Enums;
using System.Text.Json;

namespace Tansu.Application.Approvals.Commands;

public sealed record RejectCommand(Guid SheetId, string Comment) : IRequest<Unit>;

public sealed class RejectValidator : AbstractValidator<RejectCommand>
{
    public RejectValidator()
    {
        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Комментарий обязателен при отклонении.")
            .MinimumLength(3);
    }
}

public sealed class RejectHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPublishEndpoint publisher,
    IAuditRecorder audit) : IRequestHandler<RejectCommand, Unit>
{
    public async Task<Unit> Handle(RejectCommand req, CancellationToken ct)
    {
        var (sheet, employee, initiator) = await ApprovalCore.LoadCurrentStepAsync(db, req.SheetId, currentUser, ct);

        sheet.Status = ApprovalStatus.Rejected;
        sheet.Comment = req.Comment.Trim();
        sheet.DecidedAt = DateTimeOffset.UtcNow;

        var downstream = await db.ApprovalSheet
            .Where(a => a.EmployeeId == sheet.EmployeeId &&
                        a.RoundId == sheet.RoundId &&
                        a.Status == ApprovalStatus.Pending &&
                        a.OrderNo > sheet.OrderNo)
            .ToListAsync(ct);

        var skipTime = DateTimeOffset.UtcNow;
        foreach (var s in downstream)
        {
            s.Status = ApprovalStatus.Skipped;
            s.DecidedAt = skipTime;
        }

        audit.Record(new AuditEntry(
            AuditActions.EmployeeRejected, "employee", employee.Id,
            $"Отклонён сотрудник {employee.FullName}",
            JsonSerializer.Serialize(new { sheetId = sheet.Id, comment = sheet.Comment }),
            employee.ProjectOid, employee.SubcontractorId));
        await db.SaveChangesAsync(ct);

        var approver = await db.Users.AsNoTracking().FirstAsync(u => u.Id == sheet.ApproverUserId, ct);

        await publisher.Publish(new EmployeeApprovalDecisionMessage(
            employee.Id, employee.FullName,
            employee.SubcontractorId, employee.Subcontractor!.Name,
            employee.ProjectOid,
            approver.Id, approver.Email, approver.FullName,
            ApprovalStatus.Rejected,
            sheet.Comment,
            initiator.Id, initiator.Email,
            DateTimeOffset.UtcNow), ct);

        return Unit.Value;
    }
}
