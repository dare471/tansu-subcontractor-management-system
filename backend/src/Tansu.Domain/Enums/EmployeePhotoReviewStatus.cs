namespace Tansu.Domain.Enums;

public static class EmployeePhotoReviewStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

public static class EmployeePhotoReviewType
{
    public const string Auto = "auto";
    public const string Manual = "manual";
}

public static class EmployeePhotoReviewResult
{
    public const string Passed = "passed";
    public const string Failed = "failed";
    public const string Pending = "pending";
}
