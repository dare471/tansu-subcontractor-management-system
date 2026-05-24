using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Subcontractors.Commands;

public sealed record UpdateSubcontractorCommand(Guid Id, string Name, string Bin) : IRequest<SubcontractorDto>;

public sealed class UpdateSubcontractorValidator : AbstractValidator<UpdateSubcontractorCommand>
{
    public UpdateSubcontractorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Bin).NotEmpty().MaximumLength(32).Matches("^[0-9]+$");
    }
}

public sealed class UpdateSubcontractorHandler(ITansuDbContext db)
    : IRequestHandler<UpdateSubcontractorCommand, SubcontractorDto>
{
    public async Task<SubcontractorDto> Handle(UpdateSubcontractorCommand req, CancellationToken ct)
    {
        var entity = await db.Subcontractors
            .Include(x => x.Projects)
            .Include(x => x.Users)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Subcontractor", req.Id);

        if (entity.Bin != req.Bin &&
            await db.Subcontractors.AnyAsync(x => x.Bin == req.Bin && x.Id != req.Id, ct))
        {
            throw new ConflictException("bin_taken", "Другой субподрядчик уже имеет такой БИН.");
        }

        entity.Name = req.Name.Trim();
        entity.Bin = req.Bin.Trim();
        await db.SaveChangesAsync(ct);

        return new SubcontractorDto(
            entity.Id, entity.Name, entity.Bin,
            entity.Projects.Count, entity.Users.Count, entity.CreatedAt);
    }
}
