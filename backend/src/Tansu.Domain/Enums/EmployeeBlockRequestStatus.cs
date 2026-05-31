namespace Tansu.Domain.Enums;

public static class EmployeeBlockRequestStatus
{
    public const string Applied = "applied";
    public const string Rejected = "rejected";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Applied, Rejected };
}
