using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Employees.Commands;

public sealed record DeleteEmployeeCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteEmployeeHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPhotoStorage storage) : IRequestHandler<DeleteEmployeeCommand, Unit>
{
    public async Task<Unit> Handle(DeleteEmployeeCommand req, CancellationToken ct)
    {
        var e = await db.Employees.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Employee", req.Id);

        if (currentUser.UserType == UserType.Subcontractor &&
            currentUser.SubcontractorId != e.SubcontractorId)
        {
            throw new ForbiddenException("Сотрудник принадлежит другому субподрядчику.");
        }

        if (!string.IsNullOrWhiteSpace(e.PhotoPath))
            await storage.DeleteAsync(e.PhotoPath, ct);

        db.Employees.Remove(e);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
