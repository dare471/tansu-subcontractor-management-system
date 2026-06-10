using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Zup;
using Tansu.Domain.Entities;

namespace Tansu.Application.Subcontractors.Commands;

public sealed record CreateSubcontractorCommand(
    string Name,
    string Bin,
    Guid? ProjectOid = null,
    string? ProjectName = null,
    string? ActivityType = null) : IRequest<SubcontractorDto>;

public sealed class CreateSubcontractorValidator : AbstractValidator<CreateSubcontractorCommand>
{
    public CreateSubcontractorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Bin)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^[0-9]+$").WithMessage("БИН должен содержать только цифры.");
        When(x => x.ProjectOid is not null, () =>
        {
            RuleFor(x => x.ActivityType)
                .NotEmpty()
                .MaximumLength(500)
                .WithMessage("Укажите вид деятельности на проекте.");
        });
    }
}

public sealed class CreateSubcontractorHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IZupProjectDirectory zupProjects,
    IMediator mediator) : IRequestHandler<CreateSubcontractorCommand, SubcontractorDto>
{
    public async Task<SubcontractorDto> Handle(
        CreateSubcontractorCommand req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanRegisterSubcontractors, "Регистрация субподрядчиков недоступна для вашей роли.");
        accessService.EnsureCanModify(access);

        if (await db.Subcontractors.AnyAsync(x => x.Bin == req.Bin, ct))
            throw new ConflictException("bin_taken", "Субподрядчик с таким БИН уже существует.");

        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var entity = new Subcontractor
        {
            Name = req.Name.Trim(),
            Bin = req.Bin.Trim(),
            RegisteredByUserId = userId,
            ManagerUserId = userId,
            IsActive = true
        };
        db.Subcontractors.Add(entity);
        await db.SaveChangesAsync(ct);

        if (req.ProjectOid is Guid projectOid)
        {
            await ZupProjectSync.SyncToLocalRefsAsync(db, zupProjects, ct);
            await mediator.Send(new BindProjectCommand(
                entity.Id, projectOid, req.ProjectName, req.ActivityType!.Trim()), ct);
        }

        var projectsCount = await db.ProjectSubcontractors
            .CountAsync(ps => ps.SubcontractorId == entity.Id, ct);

        var managerName = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct);

        return SubcontractorMapper.ToDto(entity, projectsCount, 0, 0, managerName);
    }
}
