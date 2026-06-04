namespace Tansu.Domain.Enums;

public static class SubcontractorDocumentType
{
    public const string Contract = "contract";
    public const string License = "license";
    public const string Insurance = "insurance";
    public const string Charter = "charter";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Contract, License, Insurance, Charter, Other
    };

    public static string Label(string type) => type switch
    {
        Contract => "Договор",
        License => "Лицензия / допуск",
        Insurance => "Страхование",
        Charter => "Учредительные документы",
        Other => "Иной документ",
        _ => type
    };
}
