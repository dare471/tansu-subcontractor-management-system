namespace Tansu.Application.EmployeePhotoReviews;

public sealed class EmployeePhotoReviewOptions
{
    public const string SectionName = "EmployeePhotoReview";
    public bool RequireManualApproval { get; set; } = true;
    /// <summary>Максимальный размер JPEG-фото сотрудника в байтах (по умолчанию 1 МБ).</summary>
    public int MaxPhotoBytes { get; set; } = 1024 * 1024;
}
