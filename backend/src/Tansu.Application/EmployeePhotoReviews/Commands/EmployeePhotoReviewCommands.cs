using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeePhotoReviews.Commands;

public sealed record RunEmployeePhotoAutoReviewCommand(Guid EmployeeId) : IRequest<EmployeePhotoReviewDto>;

public sealed class RunEmployeePhotoAutoReviewHandler(
    ITansuDbContext db,
    IPhotoStorage storage,
    IReferencePhotoValidator validator,
    IOptions<EmployeePhotoReviewOptions> options) : IRequestHandler<RunEmployeePhotoAutoReviewCommand, EmployeePhotoReviewDto>
{
    public async Task<EmployeePhotoReviewDto> Handle(RunEmployeePhotoAutoReviewCommand req, CancellationToken ct)
    {
        var employee = await db.Employees.FirstAsync(e => e.Id == req.EmployeeId, ct);
        if (string.IsNullOrEmpty(employee.PhotoPath))
            throw new ValidationFailedException("Фото не загружено.");

        await using var stream = await storage.OpenReadAsync(employee.PhotoPath, ct);
        if (stream is null)
            throw new ValidationFailedException("Файл фото не найден в хранилище.");

        var validation = await validator.ValidateAsync(stream, ct);
        var detailsJson = JsonSerializer.Serialize(new
        {
            validation.Width,
            validation.Height,
            validation.FileSize,
            validation.FaceCount,
            validation.Checks
        });

        var review = new EmployeePhotoReview
        {
            EmployeeId = employee.Id,
            PhotoPath = employee.PhotoPath,
            ReviewType = EmployeePhotoReviewType.Auto,
            DetailsJson = detailsJson
        };

        if (!validation.Valid)
        {
            review.Result = EmployeePhotoReviewResult.Failed;
            review.Reason = validation.Message;
            employee.PhotoReviewStatus = EmployeePhotoReviewStatus.Rejected;
            employee.PhotoReviewReason = validation.Message;
        }
        else if (options.Value.RequireManualApproval)
        {
            review.Result = EmployeePhotoReviewResult.Pending;
            review.Reason = "Ожидает ручной проверки ответственным сотрудником ТАНСУ.";
            employee.PhotoReviewStatus = EmployeePhotoReviewStatus.Pending;
            employee.PhotoReviewReason = review.Reason;
        }
        else
        {
            review.Result = EmployeePhotoReviewResult.Passed;
            review.Reason = validation.Message;
            employee.PhotoReviewStatus = EmployeePhotoReviewStatus.Approved;
            employee.PhotoReviewReason = null;
        }

        employee.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeePhotoReviews.Add(review);
        await db.SaveChangesAsync(ct);

        return EmployeePhotoReviewMapper.ToDto(review, null);
    }
}

public sealed record ManualApproveEmployeePhotoCommand(Guid EmployeeId, string? Comment)
    : IRequest<EmployeePhotoReviewDto>;

public sealed record ManualRejectEmployeePhotoCommand(Guid EmployeeId, string Reason)
    : IRequest<EmployeePhotoReviewDto>;

public sealed class ManualApproveEmployeePhotoHandler(
    ITansuDbContext db,
    ICurrentUser currentUser) : IRequestHandler<ManualApproveEmployeePhotoCommand, EmployeePhotoReviewDto>
{
    public async Task<EmployeePhotoReviewDto> Handle(ManualApproveEmployeePhotoCommand req, CancellationToken ct)
    {
        EmployeePhotoReviewAuthorization.EnsureManualReviewer(currentUser);

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == req.EmployeeId, ct)
            ?? throw new NotFoundException("Employee", req.EmployeeId);

        if (string.IsNullOrEmpty(employee.PhotoPath))
            throw new ValidationFailedException("У сотрудника нет фото.");

        if (employee.PhotoReviewStatus != EmployeePhotoReviewStatus.Pending)
            throw new ConflictException("photo_not_pending", "Фото не ожидает ручной проверки.");

        var reviewerId = currentUser.UserId ?? throw new UnauthorizedException();
        var reviewer = await db.Users.AsNoTracking().FirstAsync(u => u.Id == reviewerId, ct);

        var review = new EmployeePhotoReview
        {
            EmployeeId = employee.Id,
            PhotoPath = employee.PhotoPath,
            ReviewType = EmployeePhotoReviewType.Manual,
            Result = EmployeePhotoReviewResult.Passed,
            Reason = string.IsNullOrWhiteSpace(req.Comment) ? "Одобрено вручную." : req.Comment.Trim(),
            ReviewedByUserId = reviewerId
        };

        employee.PhotoReviewStatus = EmployeePhotoReviewStatus.Approved;
        employee.PhotoReviewReason = null;
        employee.UpdatedAt = DateTimeOffset.UtcNow;

        db.EmployeePhotoReviews.Add(review);
        await db.SaveChangesAsync(ct);

        return EmployeePhotoReviewMapper.ToDto(review, reviewer.FullName);
    }
}

public sealed class ManualRejectEmployeePhotoHandler(
    ITansuDbContext db,
    ICurrentUser currentUser) : IRequestHandler<ManualRejectEmployeePhotoCommand, EmployeePhotoReviewDto>
{
    public async Task<EmployeePhotoReviewDto> Handle(ManualRejectEmployeePhotoCommand req, CancellationToken ct)
    {
        EmployeePhotoReviewAuthorization.EnsureManualReviewer(currentUser);

        var reason = req.Reason.Trim();
        if (string.IsNullOrEmpty(reason))
            throw new ValidationFailedException("Укажите причину отклонения.");

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == req.EmployeeId, ct)
            ?? throw new NotFoundException("Employee", req.EmployeeId);

        if (string.IsNullOrEmpty(employee.PhotoPath))
            throw new ValidationFailedException("У сотрудника нет фото.");

        var reviewerId = currentUser.UserId ?? throw new UnauthorizedException();
        var reviewer = await db.Users.AsNoTracking().FirstAsync(u => u.Id == reviewerId, ct);

        var review = new EmployeePhotoReview
        {
            EmployeeId = employee.Id,
            PhotoPath = employee.PhotoPath,
            ReviewType = EmployeePhotoReviewType.Manual,
            Result = EmployeePhotoReviewResult.Failed,
            Reason = reason,
            ReviewedByUserId = reviewerId
        };

        employee.PhotoReviewStatus = EmployeePhotoReviewStatus.Rejected;
        employee.PhotoReviewReason = reason;
        employee.UpdatedAt = DateTimeOffset.UtcNow;

        db.EmployeePhotoReviews.Add(review);
        await db.SaveChangesAsync(ct);

        return EmployeePhotoReviewMapper.ToDto(review, reviewer.FullName);
    }
}
