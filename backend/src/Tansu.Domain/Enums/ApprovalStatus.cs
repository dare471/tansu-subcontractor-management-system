namespace Tansu.Domain.Enums;

public static class ApprovalStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Skipped = "skipped";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { Pending, Approved, Rejected, Skipped };
}
