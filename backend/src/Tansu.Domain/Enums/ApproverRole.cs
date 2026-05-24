namespace Tansu.Domain.Enums;

public static class ApproverRole
{
    public const string Accounting = "accounting";
    public const string HR = "hr";
    public const string Finance = "finance";
    public const string Management = "management";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Accounting, HR, Finance, Management
    };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
