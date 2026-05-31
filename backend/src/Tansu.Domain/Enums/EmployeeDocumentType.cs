namespace Tansu.Domain.Enums;

public static class EmployeeDocumentType
{
    public const string IdCard = "id_card";
    public const string Certificate = "certificate";
    public const string SafetyBriefing = "safety_briefing";
    public const string Medical = "medical";
    public const string Permit = "permit";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        IdCard,
        Certificate,
        SafetyBriefing,
        Medical,
        Permit,
        Other
    };

    public static string Label(string type) => type switch
    {
        IdCard => "Удостоверение личности",
        Certificate => "Сертификат / допуск",
        SafetyBriefing => "Инструктаж по ТБ",
        Medical => "Медицинская справка",
        Permit => "Допуск на работы",
        Other => "Иной документ",
        _ => type
    };
}
