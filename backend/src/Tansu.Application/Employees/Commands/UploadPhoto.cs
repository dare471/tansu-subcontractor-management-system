using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeePhotoReviews;
using Tansu.Application.EmployeePhotoReviews.Commands;
using Tansu.Domain.Enums;

namespace Tansu.Application.Employees.Commands;

public sealed record UploadPhotoCommand(
    Guid EmployeeId,
    string FileName,
    Stream Content) : IRequest<UploadPhotoReviewResultDto>;

public sealed class UploadPhotoHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPhotoStorage storage,
    IMediator mediator) : IRequestHandler<UploadPhotoCommand, UploadPhotoReviewResultDto>
{
    private const int MaxBytes = 200 * 1024;

    public async Task<UploadPhotoReviewResultDto> Handle(UploadPhotoCommand req, CancellationToken ct)
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

        EnsureJpeg(req.FileName);
        if (req.Content.CanSeek && req.Content.Length > MaxBytes)
            throw new ValidationFailedException("Файл больше 200 КБ. Требование Hikvision: 40–200 КБ.");

        var relative = await storage.SaveAsync(e.Id, req.FileName, req.Content, ct);
        e.PhotoPath = relative;
        e.PhotoReviewStatus = null;
        e.PhotoReviewReason = null;
        e.PhotoUploadedByUserId = currentUser.UserId;
        e.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var review = await mediator.Send(new RunEmployeePhotoAutoReviewCommand(e.Id), ct);
        var updated = await db.Employees.AsNoTracking().FirstAsync(x => x.Id == e.Id, ct);

        return new UploadPhotoReviewResultDto(
            relative,
            updated.PhotoReviewStatus ?? EmployeePhotoReviewStatus.Rejected,
            BuildUploadMessage(updated),
            review);
    }

    internal static void EnsureJpeg(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not ".jpg" and not ".jpeg")
            throw new ValidationFailedException("Допустим только формат JPEG/JPG (требование Hikvision).");
    }

    internal static string BuildUploadMessage(Domain.Entities.Employee employee) =>
        employee.PhotoReviewStatus switch
        {
            EmployeePhotoReviewStatus.Approved =>
                "Фото прошло проверку. Можно отправлять на согласование.",
            EmployeePhotoReviewStatus.Pending =>
                "Автопроверка пройдена. Ожидается ручная проверка ТАНСУ.",
            _ => employee.PhotoReviewReason ?? "Фото не прошло проверку. Загрузите другое."
        };
}
