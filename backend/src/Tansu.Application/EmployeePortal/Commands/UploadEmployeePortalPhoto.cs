using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeePortal;
using Tansu.Application.EmployeePortal.Queries;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeePortal.Commands;

public sealed record UploadEmployeePortalPhotoCommand(string FileName, Stream Content)
    : IRequest<EmployeePortalPhotoUploadResult>;

public sealed class UploadEmployeePortalPhotoHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPhotoStorage storage,
    IFacePhotoValidator faceValidator)
    : IRequestHandler<UploadEmployeePortalPhotoCommand, EmployeePortalPhotoUploadResult>
{
    public async Task<EmployeePortalPhotoUploadResult> Handle(
        UploadEmployeePortalPhotoCommand req,
        CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Employee || currentUser.EmployeeId is null)
            throw new ForbiddenException();

        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.Id == currentUser.EmployeeId, ct)
            ?? throw new NotFoundException("Employee", currentUser.EmployeeId.Value);

        await using var buffer = new MemoryStream();
        await req.Content.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
            throw new ValidationFailedException("Файл пустой.");

        buffer.Position = 0;
        var faceCheck = await faceValidator.ValidateHasFaceAsync(buffer, ct);
        if (!faceCheck.HasFace)
            throw new ValidationFailedException(faceCheck.Message);

        buffer.Position = 0;
        var relative = await storage.SaveAsync(employee.Id, req.FileName, buffer, ct);
        employee.PhotoPath = relative;
        employee.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return new EmployeePortalPhotoUploadResult(
            relative,
            "Фото загружено. Его можно использовать для Face ID на проходной.");
    }
}

public sealed record GetEmployeePortalPhotoQuery : IRequest<Stream?>;

public sealed class GetEmployeePortalPhotoHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPhotoStorage storage) : IRequestHandler<GetEmployeePortalPhotoQuery, Stream?>
{
    public async Task<Stream?> Handle(GetEmployeePortalPhotoQuery req, CancellationToken ct)
    {
        var employee = await GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);
        if (string.IsNullOrEmpty(employee.PhotoPath))
            return null;

        return await storage.OpenReadAsync(employee.PhotoPath, ct);
    }
}
