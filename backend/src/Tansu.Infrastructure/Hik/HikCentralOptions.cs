namespace Tansu.Infrastructure.Hik;

public sealed class HikCentralOptions
{
    public const string SectionName = "HikCentral";

    public string BaseUrl { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string OrgIndexCode { get; set; } = "1";
    public IList<string> AccessGroupIndexCodes { get; set; } = new List<string>();
    public bool IgnoreCertificateErrors { get; set; }
    public HikCentralBlockMode BlockMode { get; set; } = HikCentralBlockMode.RemoveFromAccessGroups;
    public string? PositionCustomFieldId { get; set; }
    public int TimeoutSeconds { get; set; } = 60;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(AppKey)
        && !string.IsNullOrWhiteSpace(AppSecret);
}

public enum HikCentralBlockMode
{
    RemoveFromAccessGroups = 0,
    DeletePerson = 1,
}
