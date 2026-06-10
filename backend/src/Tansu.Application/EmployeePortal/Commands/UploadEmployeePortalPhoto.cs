using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeePhotoReviews;
using Tansu.Application.EmployeePhotoReviews.Commands;
using Tansu.Application.Employees.Commands;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeePortal.Commands;

public sealed record UploadEmployeePortalPhotoCommand(string FileName, Stream Content)
    : IRequest<UploadPhotoReviewResultDto>;

public sealed class UploadEmployeePortalPhotoHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPhotoStorage storage,
    IMediator mediator,
    IOptions<EmployeePhotoReviewOptions> photoOptions) : IRequestHandler<UploadEmployeePortalPhotoCommand, UploadPhotoReviewResultDto>
{
    public async Task<UploadPhotoReviewResultDto> Handle(
        UploadEmployeePortalPhotoCommand req,
        CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Employee || currentUser.EmployeeId is null)
            throw new ForbiddenException();

        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.Id == currentUser.EmployeeId, ct)
            ?? throw new NotFoundException("Employee", currentUser.EmployeeId.Value);

        UploadPhotoHandler.EnsureJpeg(req.FileName);

        await using var buffer = new MemoryStream();
        await req.Content.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
            throw new ValidationFailedException("Файл пустой.");
        var maxBytes = photoOptions.Value.MaxPhotoBytes > 0
            ? photoOptions.Value.MaxPhotoBytes
            : 1024 * 1024;
        if (buffer.Length > maxBytes)
            throw new ValidationFailedException(
                $"Файл больше {maxBytes / 1024} КБ. Уменьшите размер или измените лимит EmployeePhotoReview:MaxPhotoBytes.");

        buffer.Position = 0;
        var relative = await storage.SaveAsync(employee.Id, req.FileName, buffer, ct);
        employee.PhotoPath = relative;
        employee.PhotoReviewStatus = null;
        employee.PhotoReviewReason = null;
        employee.PhotoUploadedByUserId = currentUser.UserId;
        employee.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var review = await mediator.Send(new RunEmployeePhotoAutoReviewCommand(employee.Id), ct);
        var updated = await db.Employees.AsNoTracking().FirstAsync(e => e.Id == employee.Id, ct);

        return new UploadPhotoReviewResultDto(
            relative,
            updated.PhotoReviewStatus ?? EmployeePhotoReviewStatus.Rejected,
            UploadPhotoHandler.BuildUploadMessage(updated),
            review);
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
        var employee = await Queries.GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);
        if (string.IsNullOrEmpty(employee.PhotoPath))
            return null;

        return await storage.OpenReadAsync(employee.PhotoPath, ct);
    }
}
