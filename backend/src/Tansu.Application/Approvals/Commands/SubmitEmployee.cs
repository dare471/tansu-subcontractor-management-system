using MediatR;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Approvals.Commands;

public sealed record SubmitEmployeeCommand(Guid EmployeeId) : IRequest<Guid>;

public sealed class SubmitEmployeeHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPublishEndpoint publisher)
    : IRequestHandler<SubmitEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(SubmitEmployeeCommand req, CancellationToken ct)
    {
        var employee = await EmployeeSubmitCore.LoadEmployeeForSubmitAsync(
            db, req.EmployeeId, currentUser, ct);

        await EmployeeSubmitCore.EnsureSubmittableAsync(db, employee, null, ct);

        var initiatorId = currentUser.UserId ?? throw new UnauthorizedException();
        var initiator = await db.Users.FirstAsync(u => u.Id == initiatorId, ct);

        var prepared = await EmployeeSubmitCore.PrepareSubmissionAsync(
            db, employee, initiatorId, null, ct);

        db.ApprovalSheet.AddRange(prepared.Sheets);
        await db.SaveChangesAsync(ct);

        await EmployeeSubmitCore.PublishIndividualNotificationsAsync(
            publisher, employee, initiator, prepared, ct);

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
