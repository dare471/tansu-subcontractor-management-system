using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Contracts.Messages;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests.Commands;

public sealed record ApproveDocumentRequestCommand(Guid SheetId, string? Comment) : IRequest<Unit>;

public sealed class ApproveDocumentRequestHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPublishEndpoint publisher,
    IAuditRecorder audit) : IRequestHandler<ApproveDocumentRequestCommand, Unit>
{
    public async Task<Unit> Handle(ApproveDocumentRequestCommand req, CancellationToken ct)
    {
        var (sheet, request, initiator) = await DocumentRequestApprovalCore.LoadCurrentStepAsync(
            db, req.SheetId, currentUser, ct);

        sheet.Status = ApprovalStatus.Approved;
        sheet.Comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim();
        sheet.DecidedAt = DateTimeOffset.UtcNow;

        var nextSheet = await db.DocumentApprovalSheet
            .Where(a => a.DocumentRequestId == sheet.DocumentRequestId &&
                        a.RoundId == sheet.RoundId &&
                        a.Status == ApprovalStatus.Pending &&
                        a.OrderNo > sheet.OrderNo)
            .OrderBy(a => a.OrderNo)
            .FirstOrDefaultAsync(ct);

        audit.Record(new AuditEntry(
            AuditActions.DocumentRequestApproved, "document_request", request.Id,
            $"Заявка согласована: {request.Title}",
            ProjectOid: request.ProjectOid, SubcontractorId: request.SubcontractorId));
        await db.SaveChangesAsync(ct);

        var approver = await db.Users.AsNoTracking().FirstAsync(u => u.Id == sheet.ApproverUserId, ct);

        await publisher.Publish(new DocumentRequestDecisionMessage(
            request.Id, request.RequestType, request.Title,
            request.SubcontractorId, request.Subcontractor!.Name,
            request.ProjectOid,
            approver.Id, approver.Email, approver.FullName,
            ApprovalStatus.Approved, sheet.Comment,
            initiator.Id, initiator.Email,
            DateTimeOffset.UtcNow), ct);

        if (nextSheet is not null)
        {
            var nextApprover = await db.Users.AsNoTracking()
                .FirstAsync(u => u.Id == nextSheet.ApproverUserId, ct);

            await publisher.Publish(new DocumentRequestNextApproverMessage(
                request.Id, request.RequestType, request.Title,
                request.SubcontractorId, request.Subcontractor.Name,
                request.ProjectOid, request.Project?.Name,
                nextApprover.Id, nextApprover.Email, nextApprover.FullName,
                nextSheet.ApproverRole, nextSheet.OrderNo,
                DateTimeOffset.UtcNow), ct);
        }
        else
        {
            await publisher.Publish(new DocumentRequestFullyApprovedMessage(
                request.Id, request.RequestType, request.Title,
                request.SubcontractorId, request.Subcontractor.Name,
                request.ProjectOid,
                initiator.Id, initiator.Email,
                DateTimeOffset.UtcNow), ct);
        }

        return Unit.Value;
    }
}
