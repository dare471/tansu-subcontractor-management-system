using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Subcontractors.Commands;

public sealed record DeleteSubcontractorCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteSubcontractorHandler(ITansuDbContext db)
    : IRequestHandler<DeleteSubcontractorCommand, Unit>
{
    public async Task<Unit> Handle(DeleteSubcontractorCommand req, CancellationToken ct)
    {
        var entity = await db.Subcontractors.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Subcontractor", req.Id);

        if (await db.Users.AnyAsync(u => u.SubcontractorId == req.Id, ct))
            throw new ConflictException("has_users",
                "Сначала удалите/деактивируйте пользователей субподрядчика.");

        if (await db.Employees.AnyAsync(e => e.SubcontractorId == req.Id, ct))
            throw new ConflictException("has_employees",
                "Сначала удалите сотрудников субподрядчика.");

        db.Subcontractors.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
