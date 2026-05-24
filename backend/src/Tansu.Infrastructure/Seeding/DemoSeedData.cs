namespace Tansu.Infrastructure.Seeding;

public static class DemoSeedData
{
    public static readonly Guid ProjectKeremetOid =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static readonly Guid ProjectAbayTowerOid =
        Guid.Parse("00000000-0000-0000-0000-000000000002");

    public const string ProjectKeremetName = "ЖК «Keremet» — р-н Есиль, г. Астана";
    public const string ProjectAbayTowerName = "БЦ «Abay Tower» — г. Алматы";

    public const string SubMontazhBin = "080540012345";
    public const string SubMontazhName = "ТОО «MontazhKomplekt Astana»";

    public const string SubEnergoBin = "060701098765";
    public const string SubEnergoName = "ТОО «Qazaq EnergoStroy»";

    public const string TansuAdminEmail = "admin@tansu.local";
    public const string TansuAdminFullName = "Қайрат Сейтов";
    public const string TansuAdminPosition = "Директор департамента субподрядчиков";

    public const string SubMontazhEmail = "hr@montazh-astana.kz";
    internal const string LegacyMontazhHrEmail = "sub@example.local";
    public const string SubMontazhUserFullName = "Асхат Байжанов";
    public const string SubMontazhUserPosition = "Менеджер по персоналу";

    public const string SubEnergoEmail = "energo@qazaq-energo.kz";
    public const string SubEnergoUserFullName = "Динара Жумабекова";
    public const string SubEnergoUserPosition = "Руководитель участка";

    public const string SubcontractorTempPassword = "Montazh2024!";

    public const string DraftBatchTitle = "Бригада монтажа — Keremet";
    public const string SubmittedBatchTitle = "Допуск на объект Keremet — май";
    public const string EnergoDraftBatchTitle = "СМР Abay Tower — июнь";

    public static readonly string[] SeedMontazhEmployeeIins =
    [
        "880512301456", "900315678901", "910428765432",
        "920537894561", "930641205873", "940752316984", "950863427105"
    ];

    public static readonly string[] SeedEnergoEmployeeIins =
    [
        "860401156789", "870512267890", "880623378901"
    ];

    public static readonly string[] LegacySeedIinPrefixes = ["9900", "9910"];

    public static readonly ApproverProfile[] TansuApprovers =
    [
        new("approver1@tansu.local", "Алия Нуржанова", "Руководитель проекта"),
        new("approver2@tansu.local", "Берик Оспанов", "Инженер по ОТ и ТБ"),
        new("approver3@tansu.local", "Сауле Жұмабекова", "Директор по субподрядчикам"),
    ];

    public const string AccountingEmail = "accounting@tansu.local";
    public const string AccountingFullName = "Айгүл Сатканова";
    public const string AccountingPosition = "Главный бухгалтер";

    public sealed record ApproverProfile(string Email, string FullName, string Position);
}
