using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.DocumentRequests.Queries;

public sealed record GetDocumentMatrixQuery(
    Guid ProjectOid, Guid SubcontractorId, string RequestType)
    : IRequest<IReadOnlyList<DocumentMatrixStepDto>>;

public sealed class GetDocumentMatrixHandler(ITansuDbContext db)
    : IRequestHandler<GetDocumentMatrixQuery, IReadOnlyList<DocumentMatrixStepDto>>
{
    public async Task<IReadOnlyList<DocumentMatrixStepDto>> Handle(GetDocumentMatrixQuery req, CancellationToken ct)
    {
        if (!DocumentRequestType.IsValid(req.RequestType))
            throw new ValidationFailedException("Недопустимый тип заявки.");

        return await db.DocumentApprovalMatrix
            .AsNoTracking()
            .Where(m => m.ProjectOid == req.ProjectOid &&
                        m.SubcontractorId == req.SubcontractorId &&
                        m.RequestType == req.RequestType)
            .OrderBy(m => m.OrderNo)
            .Select(m => new DocumentMatrixStepDto(m.Id, m.OrderNo, m.ApproverRole))
            .ToListAsync(ct);
    }
}

public sealed record GetDocumentRequestApprovalsQuery(Guid RequestId)
    : IRequest<IReadOnlyList<DocumentApprovalRoundDto>>;

public sealed class GetDocumentRequestApprovalsHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetDocumentRequestApprovalsQuery, IReadOnlyList<DocumentApprovalRoundDto>>
{
    public async Task<IReadOnlyList<DocumentApprovalRoundDto>> Handle(
        GetDocumentRequestApprovalsQuery req, CancellationToken ct)
    {
        var request = await db.DocumentRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == req.RequestId, ct)
            ?? throw new NotFoundException("DocumentRequest", req.RequestId);

        if (currentUser.UserType == UserType.Subcontractor &&
            currentUser.SubcontractorId != request.SubcontractorId)
            throw new ForbiddenException();

        var sheets = await db.DocumentApprovalSheet.AsNoTracking()
            .Include(a => a.Approver)
            .Where(a => a.DocumentRequestId == req.RequestId)
            .OrderBy(a => a.RoundId)
            .ThenBy(a => a.OrderNo)
            .ToListAsync(ct);

        return sheets
            .GroupBy(s => s.RoundId)
            .Select(g => new DocumentApprovalRoundDto(
                g.Key,
                g.Min(x => x.CreatedAt),
                g.Select(s => new DocumentApprovalStepDto(
                    s.Id, s.OrderNo, s.ApproverRole,
                    s.Approver!.FullName, s.Approver.Email,
                    s.Status, s.Comment, s.DecidedAt)).ToList()))
            .OrderByDescending(r => r.StartedAt)
            .ToList();
    }
}
