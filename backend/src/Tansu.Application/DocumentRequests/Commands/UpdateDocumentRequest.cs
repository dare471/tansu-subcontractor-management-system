using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests.Commands;

public sealed record UpdateDocumentRequestCommand(
    Guid Id, string Title, string Description) : IRequest<DocumentRequestDto>;

public sealed class UpdateDocumentRequestValidator : AbstractValidator<UpdateDocumentRequestCommand>
{
    public UpdateDocumentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(4000);
    }
}

public sealed class UpdateDocumentRequestHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateDocumentRequestCommand, DocumentRequestDto>
{
    public async Task<DocumentRequestDto> Handle(UpdateDocumentRequestCommand req, CancellationToken ct)
    {
        var entity = await db.DocumentRequests.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("DocumentRequest", req.Id);

        if (currentUser.UserType == UserType.Subcontractor &&
            currentUser.SubcontractorId != entity.SubcontractorId)
            throw new ForbiddenException();

        var pending = await db.DocumentApprovalSheet.AnyAsync(
            a => a.DocumentRequestId == entity.Id && a.Status == ApprovalStatus.Pending, ct);
        if (pending)
            throw new ConflictException("approval_in_progress", "Заявка на согласовании — редактирование недоступно.");

        entity.Title = req.Title.Trim();
        entity.Description = req.Description.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return await CreateDocumentRequestHandler.MapAsync(db, entity.Id, ct);
    }
}
