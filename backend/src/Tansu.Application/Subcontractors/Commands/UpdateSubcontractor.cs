using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Subcontractors.Commands;

public sealed record UpdateSubcontractorCommand(Guid Id, string Name, string Bin, Guid? ManagerUserId)
    : IRequest<SubcontractorDto>;

public sealed class UpdateSubcontractorValidator : AbstractValidator<UpdateSubcontractorCommand>
{
    public UpdateSubcontractorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Bin).NotEmpty().MaximumLength(32).Matches("^[0-9]+$");
    }
}

public sealed class UpdateSubcontractorHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<UpdateSubcontractorCommand, SubcontractorDto>
{
    public async Task<SubcontractorDto> Handle(UpdateSubcontractorCommand req, CancellationToken ct)
    {
        await accessService.EnsureSubcontractorVisibleAsync(req.Id, ct);
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanRegisterSubcontractors || p.IsGlobalAdmin,
            "Нет права редактировать субподрядчиков.");
        accessService.EnsureCanModify(access);

        var entity = await db.Subcontractors
            .Include(x => x.Projects)
            .Include(x => x.Manager)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Subcontractor", req.Id);

        if (entity.Bin != req.Bin &&
            await db.Subcontractors.AnyAsync(x => x.Bin == req.Bin && x.Id != req.Id, ct))
        {
            throw new ConflictException("bin_taken", "Другой субподрядчик уже имеет такой БИН.");
        }

        entity.Name = req.Name.Trim();
        entity.Bin = req.Bin.Trim();

        if (req.ManagerUserId is { } managerId)
        {
            accessService.EnsurePermission(
                access,
                p => p.CanReassignSubcontractorManager || p.IsGlobalAdmin,
                "Назначение менеджера доступно администратору или глобальному администратору.");

            if (!await db.Users.AnyAsync(
                    u => u.Id == managerId && u.UserType == UserType.Tansu && u.IsActive, ct))
            {
                throw new NotFoundException("User", managerId);
            }

            entity.ManagerUserId = managerId;
        }

        await db.SaveChangesAsync(ct);
        return await SubcontractorStatsHelper.ToDtoAsync(db, entity, ct);
    }
}
