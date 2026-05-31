using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Projects.Commands;

public sealed record UpdateProjectSubcontractorBindingCommand(
    Guid ProjectOid,
    Guid SubcontractorId,
    string ActivityType)
    : IRequest<Unit>;

public sealed class UpdateProjectSubcontractorBindingValidator
    : AbstractValidator<UpdateProjectSubcontractorBindingCommand>
{
    public UpdateProjectSubcontractorBindingValidator()
    {
        RuleFor(x => x.ActivityType).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateProjectSubcontractorBindingHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService)
    : IRequestHandler<UpdateProjectSubcontractorBindingCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProjectSubcontractorBindingCommand req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Tansu)
            throw new ForbiddenException();

        var access = await accessService.GetAccessAsync(ct);
        if (access.VisibleProjectOids is { } projects && !projects.Contains(req.ProjectOid))
            throw new ForbiddenException("Проект вне вашей области видимости.");

        var link = await db.ProjectSubcontractors.FirstOrDefaultAsync(
            x => x.ProjectOid == req.ProjectOid && x.SubcontractorId == req.SubcontractorId, ct)
            ?? throw new NotFoundException("ProjectSubcontractor", req.SubcontractorId);

        link.ActivityType = req.ActivityType.Trim();
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed record ReportProjectProgressCommand(Guid ProjectOid, int CompletionPercent)
    : IRequest<Unit>;

public sealed class ReportProjectProgressValidator : AbstractValidator<ReportProjectProgressCommand>
{
    public ReportProjectProgressValidator()
    {
        RuleFor(x => x.CompletionPercent).InclusiveBetween(0, 100);
    }
}

public sealed class ReportProjectProgressHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ReportProjectProgressCommand, Unit>
{
    public async Task<Unit> Handle(ReportProjectProgressCommand req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Subcontractor)
            throw new ForbiddenException("Отчётность доступна только пользователям субподрядчика.");

        var subcontractorId = currentUser.SubcontractorId
            ?? throw new ForbiddenException("Контекст субподрядчика отсутствует.");

        var link = await db.ProjectSubcontractors.FirstOrDefaultAsync(
            x => x.ProjectOid == req.ProjectOid && x.SubcontractorId == subcontractorId, ct)
            ?? throw new ValidationFailedException("Проект не привязан к вашей организации.");

        link.CompletionPercent = req.CompletionPercent;
        link.ProgressReportedAt = DateTimeOffset.UtcNow;
        link.ProgressReportedByUserId = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
