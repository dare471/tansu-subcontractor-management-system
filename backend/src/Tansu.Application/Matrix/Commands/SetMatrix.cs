using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Matrix.Commands;

public sealed record SetMatrixCommand(
    Guid ProjectOid,
    Guid SubcontractorId,
    IReadOnlyList<MatrixStepInput> Steps) : IRequest<IReadOnlyList<MatrixStepDto>>;

public sealed class SetMatrixValidator : AbstractValidator<SetMatrixCommand>
{
    public SetMatrixValidator()
    {
        RuleFor(x => x.Steps).NotNull();
        RuleForEach(x => x.Steps).ChildRules(c =>
        {
            c.RuleFor(s => s.OrderNo).GreaterThanOrEqualTo(1);
            c.RuleFor(s => s.UserId).NotEmpty();
        });
        RuleFor(x => x.Steps).Must(s => s.Select(x => x.OrderNo).Distinct().Count() == s.Count)
            .WithMessage("Порядковые номера должны быть уникальными.");
    }
}

public sealed class SetMatrixHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IAuditRecorder audit) : IRequestHandler<SetMatrixCommand, IReadOnlyList<MatrixStepDto>>
{
    public async Task<IReadOnlyList<MatrixStepDto>> Handle(SetMatrixCommand req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Tansu)
            throw new ForbiddenException();

        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(access, p => p.CanManageApprovalMatrix, "Изменение матрицы недоступно для вашей роли.");
        accessService.EnsureCanModify(access);
        await accessService.EnsureSubcontractorVisibleAsync(req.SubcontractorId, ct);
        if (access.VisibleProjectOids is { } projects && !projects.Contains(req.ProjectOid))
            throw new ForbiddenException("Проект вне вашей области видимости.");

        if (!await db.ProjectRefs.AnyAsync(p => p.ProjectOid == req.ProjectOid, ct))
            throw new NotFoundException("Project", req.ProjectOid);

        if (!await db.Subcontractors.AnyAsync(s => s.Id == req.SubcontractorId, ct))
            throw new NotFoundException("Subcontractor", req.SubcontractorId);

        var userIds = req.Steps.Select(s => s.UserId).ToHashSet();
        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        foreach (var step in req.Steps)
            if (!users.ContainsKey(step.UserId))
                throw new NotFoundException("User", step.UserId);

        var existing = await db.ApprovalMatrix
            .Where(m => m.ProjectOid == req.ProjectOid && m.SubcontractorId == req.SubcontractorId)
            .ToListAsync(ct);

        foreach (var e in existing) db.ApprovalMatrix.Remove(e);

        var inserted = new List<ApprovalMatrixEntry>();
        foreach (var step in req.Steps.OrderBy(s => s.OrderNo))
        {
            var entry = new ApprovalMatrixEntry
            {
                OrderNo = step.OrderNo,
                ProjectOid = req.ProjectOid,
                SubcontractorId = req.SubcontractorId,
                UserId = step.UserId
            };
            db.ApprovalMatrix.Add(entry);
            inserted.Add(entry);
        }

        audit.Record(new AuditEntry(
            AuditActions.MatrixUpdated, "approval_matrix", req.ProjectOid,
            $"Матрица согласования обновлена ({req.Steps.Count} шагов)",
            ProjectOid: req.ProjectOid, SubcontractorId: req.SubcontractorId));
        await db.SaveChangesAsync(ct);

        return inserted
            .Select(m => new MatrixStepDto(
                m.Id, m.OrderNo, m.UserId, users[m.UserId].FullName, users[m.UserId].Email))
            .ToList();
    }
}
