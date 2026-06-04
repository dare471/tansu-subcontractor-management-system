namespace Tansu.Domain.Enums;

/// <summary>
/// Роли пользователей ТАНСУ (область видимости и права).
/// </summary>
public static class TansuRole
{
    public const string OidManager = "oid_manager";
    public const string OidDirector = "oid_director";
    public const string SbProject = "sb_project";
    public const string SbChief = "sb_chief";
    public const string SafetyProject = "safety_project";
    public const string SafetyChief = "safety_chief";
    public const string ProjectManager = "project_manager";
    public const string GlobalAdmin = "global_admin";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        OidManager, OidDirector, SbProject, SbChief,
        SafetyProject, SafetyChief, ProjectManager, GlobalAdmin
    };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);

    public static string Label(string role) => role switch
    {
        OidManager => "Менеджер",
        OidDirector => "Администратор",
        SbProject => "Согласующий (СБ на проекте)",
        SbChief => "Согласующий (СБ начальник)",
        SafetyProject => "Согласующий (БиОТ на проекте)",
        SafetyChief => "Согласующий (БиОТ начальник)",
        ProjectManager => "Согласующий (руководитель проекта)",
        GlobalAdmin => "Глобальный администратор",
        _ => role
    };
}
