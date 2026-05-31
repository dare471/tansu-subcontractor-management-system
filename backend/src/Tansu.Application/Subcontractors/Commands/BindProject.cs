using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Application.Subcontractors.Commands;

public sealed record BindProjectCommand(
    Guid SubcontractorId,
    Guid ProjectOid,
    string? ProjectName,
    string ActivityType)
    : IRequest<Unit>;

public sealed class BindProjectValidator : AbstractValidator<BindProjectCommand>
{
    public BindProjectValidator()
    {
        RuleFor(x => x.ActivityType).NotEmpty().MaximumLength(500);
    }
}

public sealed class BindProjectHandler(ITansuDbContext db) : IRequestHandler<BindProjectCommand, Unit>
{
    public async Task<Unit> Handle(BindProjectCommand req, CancellationToken ct)
    {
        if (!await db.Subcontractors.AnyAsync(x => x.Id == req.SubcontractorId, ct))
            throw new NotFoundException("Subcontractor", req.SubcontractorId);

        var project = await db.ProjectRefs.FirstOrDefaultAsync(p => p.ProjectOid == req.ProjectOid, ct);
        if (project is null)
        {
            project = new ProjectRef { ProjectOid = req.ProjectOid, Name = req.ProjectName };
            db.ProjectRefs.Add(project);
        }
        else if (!string.IsNullOrWhiteSpace(req.ProjectName) && project.Name != req.ProjectName)
        {
            project.Name = req.ProjectName;
        }

        var link = await db.ProjectSubcontractors.FirstOrDefaultAsync(
            x => x.ProjectOid == req.ProjectOid && x.SubcontractorId == req.SubcontractorId, ct);

        if (link is null)
        {
            db.ProjectSubcontractors.Add(new ProjectSubcontractor
            {
                ProjectOid = req.ProjectOid,
                SubcontractorId = req.SubcontractorId,
                ActivityType = req.ActivityType.Trim()
            });
        }
        else
        {
            link.ActivityType = req.ActivityType.Trim();
        }

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed record UnbindProjectCommand(Guid SubcontractorId, Guid ProjectOid) : IRequest<Unit>;

public sealed class UnbindProjectHandler(ITansuDbContext db) : IRequestHandler<UnbindProjectCommand, Unit>
{
    public async Task<Unit> Handle(UnbindProjectCommand req, CancellationToken ct)
    {
        var link = await db.ProjectSubcontractors.FirstOrDefaultAsync(
            x => x.ProjectOid == req.ProjectOid && x.SubcontractorId == req.SubcontractorId, ct);
        if (link is null) return Unit.Value;

        db.ProjectSubcontractors.Remove(link);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
