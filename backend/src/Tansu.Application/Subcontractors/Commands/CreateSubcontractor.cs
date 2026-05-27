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

public sealed class CreateSubcontractorHandler(ITansuDbContext db)
    : IRequestHandler<CreateSubcontractorCommand, SubcontractorDto>
{
    public async Task<SubcontractorDto> Handle(
        CreateSubcontractorCommand req, CancellationToken ct)
    {
        if (await db.Subcontractors.AnyAsync(x => x.Bin == req.Bin, ct))
            throw new ConflictException("bin_taken", "Субподрядчик с таким БИН уже существует.");

        var entity = new Subcontractor { Name = req.Name.Trim(), Bin = req.Bin.Trim() };
        db.Subcontractors.Add(entity);
        await db.SaveChangesAsync(ct);

        return new SubcontractorDto(entity.Id, entity.Name, entity.Bin, 0, 0, 0, entity.CreatedAt);
    }
}
