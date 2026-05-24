using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests.Commands;

public sealed record CreateDocumentRequestCommand(
    Guid ProjectOid,
    string RequestType,
    string Title,
    string Description) : IRequest<DocumentRequestDto>;

public sealed class CreateDocumentRequestValidator : AbstractValidator<CreateDocumentRequestCommand>
{
    public CreateDocumentRequestValidator()
    {
        RuleFor(x => x.ProjectOid).NotEmpty();
        RuleFor(x => x.RequestType).Must(DocumentRequestType.IsValid)
            .WithMessage("Недопустимый тип заявки.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(4000);
    }
}

public sealed class CreateDocumentRequestHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateDocumentRequestCommand, DocumentRequestDto>
{
    public async Task<DocumentRequestDto> Handle(CreateDocumentRequestCommand req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Subcontractor)
            throw new ForbiddenException("Создавать заявки может только субподрядчик.");

        var sid = currentUser.SubcontractorId
            ?? throw new ForbiddenException("Контекст субподрядчика отсутствует.");

        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var bound = await db.ProjectSubcontractors.AnyAsync(
            x => x.SubcontractorId == sid && x.ProjectOid == req.ProjectOid, ct);
        if (!bound)
            throw new ValidationFailedException("Проект не привязан к вашей организации.");

        var entity = new DocumentRequest
        {
            SubcontractorId = sid,
            ProjectOid = req.ProjectOid,
            CreatedByUserId = userId,
            RequestType = req.RequestType,
            Title = req.Title.Trim(),
            Description = req.Description.Trim()
        };

        db.DocumentRequests.Add(entity);
        await db.SaveChangesAsync(ct);

        return await MapAsync(db, entity.Id, ct);
    }

    internal static async Task<DocumentRequestDto> MapAsync(ITansuDbContext db, Guid id, CancellationToken ct)
    {
        var e = await db.DocumentRequests.AsNoTracking()
            .Include(x => x.Subcontractor)
            .Include(x => x.Project)
            .FirstAsync(x => x.Id == id, ct);

        var sheets = await db.DocumentApprovalSheet.AsNoTracking()
            .Include(a => a.Approver)
            .Where(a => a.DocumentRequestId == id)
            .ToListAsync(ct);

        var (status, approverName, approverRole, stepNo) =
            DocumentRequestStatusResolver.Resolve(sheets);

        return new DocumentRequestDto(
            e.Id, e.SubcontractorId, e.Subcontractor!.Name, e.ProjectOid, e.Project!.Name,
            e.RequestType, e.Title, e.Description,
            status, approverName, approverRole, stepNo,
            e.CreatedAt, e.UpdatedAt);
    }
}
