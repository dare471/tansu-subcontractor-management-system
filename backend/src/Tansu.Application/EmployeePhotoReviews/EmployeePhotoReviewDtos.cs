namespace Tansu.Application.EmployeePhotoReviews;

public sealed record EmployeePhotoReviewDto(
    Guid Id,
    Guid EmployeeId,
    string PhotoPath,
    string ReviewType,
    string Result,
    string? Reason,
    string? ReviewedByFullName,
    DateTimeOffset CreatedAt);

public sealed record EmployeePhotoReviewStatusDto(
    Guid EmployeeId,
    string? PhotoPath,
    string? Status,
    string? Reason,
    bool CanSubmitForApproval,
    IReadOnlyList<EmployeePhotoReviewDto> History);

public sealed record PendingPhotoReviewItemDto(
    Guid EmployeeId,
    string FullName,
    string Position,
    string SubcontractorName,
    string? ProjectName,
    string PhotoPath,
    DateTimeOffset UploadedAt);

public sealed record ManualPhotoReviewRequest(string? Comment);

public sealed record UploadPhotoReviewResultDto(
    string PhotoPath,
    string Status,
    string Message,
    EmployeePhotoReviewDto? LatestReview);
