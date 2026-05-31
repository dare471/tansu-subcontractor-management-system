namespace Tansu.Domain.Enums;

public static class ApproverRole
{
    public const string Accounting = "accounting";
    public const string HR = "hr";
    public const string Finance = "finance";
    public const string Management = "management";
    public const string OID = "oid";
    public const string Safety = "safety";
    public const string Security = "security";

    public static readonly IReadOnlySet<string> BlockInitiatorRoles = new HashSet<string>
    {
        OID, Safety, Security
    };

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Accounting, HR, Finance, Management, OID, Safety, Security
    };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);

    public static bool CanInitiateEmployeeBlock(string? role, bool isSuperUser) =>
        isSuperUser || (role is not null && BlockInitiatorRoles.Contains(role));

    public static string Label(string role) => role switch
    {
        Accounting => "Бухгалтерия",
        HR => "HR",
        Finance => "Финансы",
        Management => "Руководство",
        OID => "ОИД",
        Safety => "БиОТ/ТБ",
        Security => "СБ",
        _ => role
    };
}
