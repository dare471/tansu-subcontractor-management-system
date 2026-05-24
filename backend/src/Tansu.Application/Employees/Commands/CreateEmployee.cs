using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Application.Employees.Commands;

public sealed record CreateEmployeeCommand(
    Guid SubcontractorId,
    Guid ProjectOid,
    string FullName,
    string Position,
    string Phone,
    string Iin) : IRequest<EmployeeDto>;

public sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Iin)
            .NotEmpty()
            .Matches("^[0-9]{12}$").WithMessage("ИИН должен состоять из 12 цифр.");
    }
}

public sealed class CreateEmployeeHandler(ITansuDbContext db)
    : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(CreateEmployeeCommand req, CancellationToken ct)
    {
        var sub = await db.Subcontractors.FirstOrDefaultAsync(s => s.Id == req.SubcontractorId, ct)
            ?? throw new NotFoundException("Subcontractor", req.SubcontractorId);

        var hasBinding = await db.ProjectSubcontractors.AnyAsync(
            ps => ps.ProjectOid == req.ProjectOid && ps.SubcontractorId == req.SubcontractorId, ct);
        if (!hasBinding)
            throw new ValidationFailedException(
                "Субподрядчик не привязан к этому проекту.");

        var project = await db.ProjectRefs.FirstOrDefaultAsync(p => p.ProjectOid == req.ProjectOid, ct);

        var e = new Employee
        {
            SubcontractorId = req.SubcontractorId,
            ProjectOid = req.ProjectOid,
            FullName = req.FullName.Trim(),
            Position = req.Position.Trim(),
            Phone = req.Phone.Trim(),
            Iin = req.Iin.Trim()
        };
        db.Employees.Add(e);
        await db.SaveChangesAsync(ct);

        return new EmployeeDto(
            e.Id, e.SubcontractorId, sub.Name,
            e.ProjectOid, project?.Name,
            e.FullName, e.Position, e.Phone, e.Iin, e.PhotoPath,
            null, null, null, null, null,
            e.CreatedAt, e.UpdatedAt);
    }
}
