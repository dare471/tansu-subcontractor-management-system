namespace Tansu.Domain.Enums;

public static class ProjectDocumentType
{
    public const string Contract = "contract";
    public const string Estimate = "estimate";
    public const string Act = "act";
    public const string Permit = "permit";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Contract, Estimate, Act, Permit, Other
    };

    public static string Label(string type) => type switch
    {
        Contract => "Договор",
        Estimate => "Смета",
        Act => "Акт",
        Permit => "Разрешение",
        _ => "Прочее"
    };
}
