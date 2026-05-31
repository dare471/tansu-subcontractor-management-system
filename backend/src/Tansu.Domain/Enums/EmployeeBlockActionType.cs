namespace Tansu.Domain.Enums;

public static class EmployeeBlockActionType
{
    public const string Block = "block";
    public const string Unblock = "unblock";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Block,
        Unblock
    };
}
