using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Contracts.Messages;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests.Commands;

public sealed record SubmitDocumentRequestCommand(Guid RequestId) : IRequest<Guid>;

public sealed class SubmitDocumentRequestHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPublishEndpoint publisher) : IRequestHandler<SubmitDocumentRequestCommand, Guid>
{
    public async Task<Guid> Handle(SubmitDocumentRequestCommand req, CancellationToken ct)
    {
        var request = await db.DocumentRequests
            .Include(r => r.Subcontractor)
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == req.RequestId, ct)
            ?? throw new NotFoundException("DocumentRequest", req.RequestId);

        if (currentUser.UserType != UserType.Subcontractor ||
            currentUser.SubcontractorId != request.SubcontractorId)
            throw new ForbiddenException("Только субподрядчик-владелец может отправить заявку.");

        var anyPending = await db.DocumentApprovalSheet.AnyAsync(
            a => a.DocumentRequestId == request.Id && a.Status == ApprovalStatus.Pending, ct);
        if (anyPending)
            throw new ConflictException("approval_in_progress", "Заявка уже на согласовании.");

        var matrix = await db.DocumentApprovalMatrix
            .Where(m => m.ProjectOid == request.ProjectOid &&
                        m.SubcontractorId == request.SubcontractorId &&
                        m.RequestType == request.RequestType)
            .OrderBy(m => m.OrderNo)
            .ToListAsync(ct);

        if (matrix.Count == 0)
            throw new ValidationFailedException(
                "Не настроена матрица согласования для этого типа заявки и проекта.");

        var roleApprovers = await DocumentRequestApprovalCore.ResolveRoleApproversAsync(
            db, matrix.Select(m => m.ApproverRole), ct);

        var initiatorId = currentUser.UserId ?? throw new UnauthorizedException();
        var initiator = await db.Users.FirstAsync(u => u.Id == initiatorId, ct);

        var roundId = Guid.NewGuid();
        var sheets = new List<DocumentApprovalSheetEntry>();

        foreach (var step in matrix)
        {
            var approver = roleApprovers[step.ApproverRole];
            sheets.Add(new DocumentApprovalSheetEntry
            {
                DocumentRequestId = request.Id,
                ApproverUserId = approver.Id,
                ApproverRole = step.ApproverRole,
                OrderNo = step.OrderNo,
                RoundId = roundId,
                Status = ApprovalStatus.Pending
            });
        }

        request.UpdatedAt = DateTimeOffset.UtcNow;
        db.DocumentApprovalSheet.AddRange(sheets);
        await db.SaveChangesAsync(ct);

        var first = sheets.OrderBy(s => s.OrderNo).First();
        var firstApprover = roleApprovers[first.ApproverRole];

        await publisher.Publish(new DocumentRequestSubmittedMessage(
            request.Id, request.RequestType, request.Title,
            request.SubcontractorId, request.Subcontractor!.Name,
            request.ProjectOid, request.Project?.Name,
            initiator.Id, initiator.Email,
            firstApprover.Id, firstApprover.Email, firstApprover.FullName,
            first.ApproverRole,
            DateTimeOffset.UtcNow), ct);

        await publisher.Publish(new DocumentRequestNextApproverMessage(
            request.Id, request.RequestType, request.Title,
            request.SubcontractorId, request.Subcontractor.Name,
            request.ProjectOid, request.Project?.Name,
            firstApprover.Id, firstApprover.Email, firstApprover.FullName,
            first.ApproverRole, first.OrderNo,
            DateTimeOffset.UtcNow), ct);

        return roundId;
    }
}

public sealed record ResubmitDocumentRequestCommand(Guid RequestId) : IRequest<Guid>;

public sealed class ResubmitDocumentRequestHandler(IMediator mediator)
    : IRequestHandler<ResubmitDocumentRequestCommand, Guid>
{
    public Task<Guid> Handle(ResubmitDocumentRequestCommand req, CancellationToken ct) =>
        mediator.Send(new SubmitDocumentRequestCommand(req.RequestId), ct);
}
