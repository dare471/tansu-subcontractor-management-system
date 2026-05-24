using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Application.Subcontractors.Commands;

public sealed record BindProjectCommand(Guid SubcontractorId, Guid ProjectOid, string? ProjectName)
    : IRequest<Unit>;

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

        var exists = await db.ProjectSubcontractors.AnyAsync(
            x => x.ProjectOid == req.ProjectOid && x.SubcontractorId == req.SubcontractorId, ct);

        if (!exists)
        {
            db.ProjectSubcontractors.Add(new ProjectSubcontractor
            {
                ProjectOid = req.ProjectOid,
                SubcontractorId = req.SubcontractorId
            });
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
