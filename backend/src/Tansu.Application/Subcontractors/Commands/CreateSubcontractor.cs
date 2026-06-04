using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Application.Subcontractors.Commands;

public sealed record CreateSubcontractorCommand(string Name, string Bin) : IRequest<SubcontractorDto>;

public sealed class CreateSubcontractorValidator : AbstractValidator<CreateSubcontractorCommand>
{
    public CreateSubcontractorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Bin)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^[0-9]+$").WithMessage("БИН должен содержать только цифры.");
    }
}

public sealed class CreateSubcontractorHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<CreateSubcontractorCommand, SubcontractorDto>
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

        var managerName = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct);

        return SubcontractorMapper.ToDto(entity, 0, 0, 0, managerName);
    }
}
