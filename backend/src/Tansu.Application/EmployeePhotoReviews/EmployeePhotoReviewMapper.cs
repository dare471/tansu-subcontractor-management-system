using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeePhotoReviews;

internal static class EmployeePhotoReviewAuthorization
{
    public static void EnsureManualReviewer(ICurrentUser currentUser)
    {
        if (currentUser.UserType != UserType.Tansu)
            throw new ForbiddenException("Ручная проверка фото доступна только сотрудникам ТАНСУ.");
    }
}

internal static class EmployeePhotoReviewMapper
{
    public static EmployeePhotoReviewDto ToDto(Domain.Entities.EmployeePhotoReview review, string? reviewerName) =>
        new(
            review.Id,
            review.EmployeeId,
            review.PhotoPath,
            review.ReviewType,
            review.Result,
            review.Reason,
            reviewerName ?? review.ReviewedBy?.FullName,
            review.CreatedAt);

    public static EmployeePhotoReviewStatusDto ToStatusDto(Domain.Entities.Employee employee, IReadOnlyList<EmployeePhotoReviewDto> history) =>
        new(
            employee.Id,
            employee.PhotoPath,
            employee.PhotoReviewStatus,
            employee.PhotoReviewReason,
            employee.PhotoReviewStatus == EmployeePhotoReviewStatus.Approved
                && !string.IsNullOrEmpty(employee.PhotoPath),
            history);
}
