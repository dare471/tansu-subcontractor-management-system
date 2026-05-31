namespace Tansu.Domain.Enums;

public static class SiteVisitDataSource
{
    public const string FaceId = "face_id";
    public const string HikTerminal = "hik_terminal";
    public const string Manual = "manual";

    public static string Label(string? value) => value switch
    {
        FaceId => "Face ID",
        HikTerminal => "Терминал HikVision",
        Manual => "Ручной ввод",
        _ => value ?? "—"
    };
}
