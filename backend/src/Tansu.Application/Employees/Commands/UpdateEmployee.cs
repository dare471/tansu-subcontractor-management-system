using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Employees.Commands;

public sealed record UpdateEmployeeCommand(
    Guid Id,
    string FullName,
    string Position,
    string Phone,
    string Iin) : IRequest<EmployeeDto>;

public sealed class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Iin).NotEmpty().Matches("^[0-9]{12}$");
    }
}

public sealed class UpdateEmployeeHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateEmployeeCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(UpdateEmployeeCommand req, CancellationToken ct)
    {
        var e = await db.Employees
            .Include(x => x.Subcontractor)
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Employee", req.Id);

        if (currentUser.UserType == UserType.Subcontractor &&
            currentUser.SubcontractorId != e.SubcontractorId)
        {
            throw new ForbiddenException("Сотрудник принадлежит другому субподрядчику.");
        }

        e.FullName = req.FullName.Trim();
        e.Position = req.Position.Trim();
        e.Phone = req.Phone.Trim();
        e.Iin = req.Iin.Trim();
        e.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return new EmployeeDto(
            e.Id, e.SubcontractorId, e.Subcontractor!.Name,
            e.ProjectOid, e.Project?.Name,
            e.FullName, e.Position, e.Phone, e.Iin, e.PhotoPath,
            null, null, null, null, null,
            e.CreatedAt, e.UpdatedAt);
    }
}
