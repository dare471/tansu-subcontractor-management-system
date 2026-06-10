using MediatR;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.AccessPasses.Commands;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Delegations;
using Tansu.Application.EmployeePortal.Commands;
using Tansu.Domain.Enums;

namespace Tansu.Application.Approvals.Commands;

public sealed record SubmitEmployeeCommand(Guid EmployeeId) : IRequest<Guid>;

public sealed class SubmitEmployeeHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPublishEndpoint publisher,
    IMediator mediator,
    IAuditRecorder audit)
    : IRequestHandler<SubmitEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(SubmitEmployeeCommand req, CancellationToken ct)
    {
        var employee = await EmployeeSubmitCore.LoadEmployeeForSubmitAsync(
            db, req.EmployeeId, currentUser, ct);

        await mediator.Send(new RevokeEmployeeAccessPassesCommand(req.EmployeeId), ct);
        await mediator.Send(new EmployeePortal.Commands.DeactivateEmployeePortalCommand(req.EmployeeId), ct);

        await EmployeeSubmitCore.EnsurePhotoApprovedAsync(db, employee, mediator, ct);
        await EmployeeSubmitCore.EnsureSubmittableAsync(db, employee, null, ct);

        var initiatorId = currentUser.UserId ?? throw new UnauthorizedException();
        var initiator = await db.Users.FirstAsync(u => u.Id == initiatorId, ct);

        var prepared = await EmployeeSubmitCore.PrepareSubmissionAsync(
            db, employee, initiatorId, null, ct);

        foreach (var sheet in prepared.Sheets)
            await DelegationResolver.ApplyToEmployeeSheetAsync(db, sheet, employee, ct);
        db.ApprovalSheet.AddRange(prepared.Sheets);
        audit.Record(new AuditEntry(
            AuditActions.EmployeeSubmitted, "employee", employee.Id,
            $"Отправлен на согласование: {employee.FullName}",
            ProjectOid: employee.ProjectOid, SubcontractorId: employee.SubcontractorId));
        await db.SaveChangesAsync(ct);

        await EmployeeSubmitCore.PublishIndividualNotificationsAsync(
            publisher, employee, initiator, prepared, ct);

        await mediator.Send(new ProvisionEmployeePortalCommand(req.EmployeeId), ct);

        return prepared.RoundId;
    }
}

public sealed record ResubmitEmployeeCommand(Guid EmployeeId) : IRequest<Guid>;

public sealed class ResubmitEmployeeHandler(IMediator mediator)
    : IRequestHandler<ResubmitEmployeeCommand, Guid>
{
    public Task<Guid> Handle(ResubmitEmployeeCommand req, CancellationToken ct) =>
        mediator.Send(new SubmitEmployeeCommand(req.EmployeeId), ct);
}
