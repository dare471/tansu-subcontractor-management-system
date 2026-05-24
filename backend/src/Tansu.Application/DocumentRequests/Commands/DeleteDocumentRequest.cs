using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests.Commands;

public sealed record DeleteDocumentRequestCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteDocumentRequestHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<DeleteDocumentRequestCommand, Unit>
{
    public async Task<Unit> Handle(DeleteDocumentRequestCommand req, CancellationToken ct)
    {
        var entity = await db.DocumentRequests.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("DocumentRequest", req.Id);

        if (currentUser.UserType == UserType.Subcontractor &&
            currentUser.SubcontractorId != entity.SubcontractorId)
            throw new ForbiddenException();

        var anySheet = await db.DocumentApprovalSheet.AnyAsync(
            a => a.DocumentRequestId == entity.Id, ct);
        if (anySheet)
            throw new ConflictException("has_approvals", "Нельзя удалить заявку, которая уже отправлялась на согласование.");

        db.DocumentRequests.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
