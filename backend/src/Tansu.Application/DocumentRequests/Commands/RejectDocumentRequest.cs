using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Contracts.Messages;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests.Commands;

public sealed record RejectDocumentRequestCommand(Guid SheetId, string Comment) : IRequest<Unit>;

public sealed class RejectDocumentRequestValidator : AbstractValidator<RejectDocumentRequestCommand>
{
    public RejectDocumentRequestValidator()
    {
        RuleFor(x => x.Comment).NotEmpty().MinimumLength(3);
    }
}

public sealed class RejectDocumentRequestHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPublishEndpoint publisher) : IRequestHandler<RejectDocumentRequestCommand, Unit>
{
    public async Task<Unit> Handle(RejectDocumentRequestCommand req, CancellationToken ct)
    {
        var (sheet, request, initiator) = await DocumentRequestApprovalCore.LoadCurrentStepAsync(
            db, req.SheetId, currentUser, ct);

        sheet.Status = ApprovalStatus.Rejected;
        sheet.Comment = req.Comment.Trim();
        sheet.DecidedAt = DateTimeOffset.UtcNow;

        var downstream = await db.DocumentApprovalSheet
            .Where(a => a.DocumentRequestId == sheet.DocumentRequestId &&
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

        await db.SaveChangesAsync(ct);

        var approver = await db.Users.AsNoTracking().FirstAsync(u => u.Id == sheet.ApproverUserId, ct);

        await publisher.Publish(new DocumentRequestDecisionMessage(
            request.Id, request.RequestType, request.Title,
            request.SubcontractorId, request.Subcontractor!.Name,
            request.ProjectOid,
            approver.Id, approver.Email, approver.FullName,
            ApprovalStatus.Rejected, sheet.Comment,
            initiator.Id, initiator.Email,
            DateTimeOffset.UtcNow), ct);

        return Unit.Value;
    }
}
