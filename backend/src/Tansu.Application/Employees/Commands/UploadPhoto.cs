using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Employees.Commands;

public sealed record UploadPhotoCommand(
    Guid EmployeeId,
    string FileName,
    Stream Content) : IRequest<string>;

public sealed class UploadPhotoHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPhotoStorage storage) : IRequestHandler<UploadPhotoCommand, string>
{
    public async Task<string> Handle(UploadPhotoCommand req, CancellationToken ct)
    {
        var e = await db.Employees.FirstOrDefaultAsync(x => x.Id == req.EmployeeId, ct)
            ?? throw new NotFoundException("Employee", req.EmployeeId);

        if (currentUser.UserType == UserType.Employee &&
            currentUser.EmployeeId != req.EmployeeId)
        {
            throw new ForbiddenException();
        }

        if (currentUser.UserType == UserType.Subcontractor &&
            currentUser.SubcontractorId != e.SubcontractorId)
        {
            throw new ForbiddenException("Сотрудник принадлежит другому субподрядчику.");
        }

        var relative = await storage.SaveAsync(e.Id, req.FileName, req.Content, ct);
        e.PhotoPath = relative;
        e.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return relative;
    }
}
