namespace Tansu.Domain.Enums;

/// <summary>Внутренние компании ТАНСУ (учётные записи сотрудников из ЗУП).</summary>
public static class TansuEmployerCompany
{
    public const string TansuConstruction = "tansu_construction";
    public const string KazPromService = "kazprom_service";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        TansuConstruction,
        KazPromService
    };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);

    public static string Label(string company) => company switch
    {
        TansuConstruction => "ТОО TANSU Construction",
        KazPromService => "ТОО KazPromService",
        _ => company
    };

    /// <summary>Значение query-параметра company для API ЗУП.</summary>
    public static string ZupQueryValue(string company) => company switch
    {
        TansuConstruction => "ТОО TANSU Construction",
        KazPromService => "ТОО KazPromService",
        _ => company
    };
}
