using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.AccessPasses.Commands;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeeDocuments.Commands;
using Tansu.Contracts.Messages;
using Tansu.Domain.Enums;

namespace Tansu.Application.Approvals.Commands;

public sealed record ApproveCommand(Guid SheetId, string? Comment) : IRequest<Unit>;

public sealed class ApproveHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPublishEndpoint publisher,
    IMediator mediator) : IRequestHandler<ApproveCommand, Unit>
{
    public async Task<Unit> Handle(ApproveCommand req, CancellationToken ct)
    {
        var (sheet, employee, initiator) = await ApprovalCore.LoadCurrentStepAsync(db, req.SheetId, currentUser, ct);

        sheet.Status = ApprovalStatus.Approved;
        sheet.Comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim();
        sheet.DecidedAt = DateTimeOffset.UtcNow;

        var nextSheet = await db.ApprovalSheet
            .Where(a => a.EmployeeId == sheet.EmployeeId &&
                        a.RoundId == sheet.RoundId &&
                        a.Status == ApprovalStatus.Pending &&
                        a.OrderNo > sheet.OrderNo)
            .OrderBy(a => a.OrderNo)
            .FirstOrDefaultAsync(ct);

        await db.SaveChangesAsync(ct);

        var approver = await db.Users.AsNoTracking().FirstAsync(u => u.Id == sheet.ApproverUserId, ct);

        await publisher.Publish(new EmployeeApprovalDecisionMessage(
            employee.Id, employee.FullName,
            employee.SubcontractorId, employee.Subcontractor!.Name,
            employee.ProjectOid,
            approver.Id, approver.Email, approver.FullName,
            ApprovalStatus.Approved,
            sheet.Comment,
            initiator.Id, initiator.Email,
            DateTimeOffset.UtcNow), ct);

        if (nextSheet is not null)
        {
            var nextApprover = await db.Users.AsNoTracking()
                .FirstAsync(u => u.Id == nextSheet.ApproverUserId, ct);

            await publisher.Publish(new NextApproverNotificationMessage(
                employee.Id, employee.FullName,
                employee.SubcontractorId, employee.Subcontractor!.Name,
                employee.ProjectOid,
                nextApprover.Id, nextApprover.Email, nextApprover.FullName,
                nextSheet.OrderNo,
                DateTimeOffset.UtcNow), ct);
        }
        else
        {
            await publisher.Publish(new EmployeeFullyApprovedMessage(
                employee.Id, employee.FullName,
                employee.SubcontractorId, employee.Subcontractor!.Name,
                employee.ProjectOid,
                initiator.Id, initiator.Email,
                DateTimeOffset.UtcNow), ct);

            await mediator.Send(
                new UnblockEmployeeAfterReapprovalCommand(employee.Id, initiator.Id), ct);
            await mediator.Send(new IssueEmployeeAccessPassCommand(employee.Id), ct);
            await mediator.Send(new EmployeePortal.Commands.ProvisionEmployeePortalCommand(employee.Id), ct);
        }

        return Unit.Value;
    }
}
