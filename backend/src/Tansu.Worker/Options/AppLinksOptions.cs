namespace Tansu.Worker.Options;

public class AppLinksOptions
{
    public const string SectionName = "App";
    public string WebBaseUrl { get; set; } = "http://localhost:5173";
    public string? EmployeePortalBaseUrl { get; set; } = "http://localhost:5175";
}
