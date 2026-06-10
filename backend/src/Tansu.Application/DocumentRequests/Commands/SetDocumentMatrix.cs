using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests.Commands;

public sealed record SetDocumentMatrixCommand(
    Guid ProjectOid,
    Guid SubcontractorId,
    string RequestType,
    IReadOnlyList<DocumentMatrixStepInput> Steps) : IRequest<IReadOnlyList<DocumentMatrixStepDto>>;

public sealed class SetDocumentMatrixValidator : AbstractValidator<SetDocumentMatrixCommand>
{
    public SetDocumentMatrixValidator()
    {
        RuleFor(x => x.RequestType).Must(DocumentRequestType.IsValid);
        RuleFor(x => x.Steps).NotNull();
        RuleForEach(x => x.Steps).ChildRules(c =>
        {
            c.RuleFor(s => s.OrderNo).GreaterThanOrEqualTo(1);
            c.RuleFor(s => s.ApproverRole).Must(ApproverRole.IsValid);
        });
        RuleFor(x => x.Steps).Must(s => s.Select(x => x.OrderNo).Distinct().Count() == s.Count)
            .WithMessage("Порядковые номера должны быть уникальными.");
    }
}

public sealed class SetDocumentMatrixHandler(
    ITansuDbContext db,
    IAuditRecorder audit)
    : IRequestHandler<SetDocumentMatrixCommand, IReadOnlyList<DocumentMatrixStepDto>>
{
    public async Task<IReadOnlyList<DocumentMatrixStepDto>> Handle(SetDocumentMatrixCommand req, CancellationToken ct)
    {
        if (!await db.ProjectRefs.AnyAsync(p => p.ProjectOid == req.ProjectOid, ct))
            throw new NotFoundException("Project", req.ProjectOid);

        if (!await db.Subcontractors.AnyAsync(s => s.Id == req.SubcontractorId, ct))
            throw new NotFoundException("Subcontractor", req.SubcontractorId);

        var existing = await db.DocumentApprovalMatrix
            .Where(m => m.ProjectOid == req.ProjectOid &&
                        m.SubcontractorId == req.SubcontractorId &&
                        m.RequestType == req.RequestType)
            .ToListAsync(ct);

        foreach (var e in existing)
            db.DocumentApprovalMatrix.Remove(e);

        var inserted = new List<DocumentRequestMatrixEntry>();
        foreach (var step in req.Steps.OrderBy(s => s.OrderNo))
        {
            var entry = new DocumentRequestMatrixEntry
            {
                ProjectOid = req.ProjectOid,
                SubcontractorId = req.SubcontractorId,
                RequestType = req.RequestType,
                OrderNo = step.OrderNo,
                ApproverRole = step.ApproverRole
            };
            db.DocumentApprovalMatrix.Add(entry);
            inserted.Add(entry);
        }

        audit.Record(new AuditEntry(
            AuditActions.DocumentMatrixUpdated, "document_matrix", req.ProjectOid,
            $"Матрица заявок ({req.RequestType}) обновлена",
            ProjectOid: req.ProjectOid, SubcontractorId: req.SubcontractorId));
        await db.SaveChangesAsync(ct);

        return inserted
            .Select(m => new DocumentMatrixStepDto(m.Id, m.OrderNo, m.ApproverRole))
            .ToList();
    }
}
