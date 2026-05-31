namespace Tansu.Application.EmployeePhotoReviews;

public sealed class EmployeePhotoReviewOptions
{
    public const string SectionName = "EmployeePhotoReview";
    public bool RequireManualApproval { get; set; } = true;
}
