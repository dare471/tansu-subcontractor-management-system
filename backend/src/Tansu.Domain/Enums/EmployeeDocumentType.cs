namespace Tansu.Domain.Enums;

public static class EmployeeDocumentType
{
    public const string IdCard = "id_card";
    public const string Certificate = "certificate";
    public const string SafetyBriefing = "safety_briefing";
    public const string Medical = "medical";
    public const string MedicalForm086 = "medical_form_086";
    public const string ElectricianLicense = "electrician_license";
    public const string Permit = "permit";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        IdCard,
        Certificate,
        SafetyBriefing,
        Medical,
        MedicalForm086,
        ElectricianLicense,
        Permit,
        Other
    };

    public static string Label(string type) => type switch
    {
        IdCard => "Удостоверение личности",
        Certificate => "Сертификат / допуск",
        SafetyBriefing => "Инструктаж по ТБ",
        Medical => "Медицинская справка",
        MedicalForm086 => "Форма 086 (медосмотр)",
        ElectricianLicense => "Удостоверение электрика",
        Permit => "Допуск на работы",
        Other => "Иной документ",
        _ => type
    };
}
