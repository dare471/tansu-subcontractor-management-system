namespace Tansu.Infrastructure.Auth;

public class EntraOptions
{
    public const string SectionName = "Entra";

    public string TenantId { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public string Authority =>
        string.IsNullOrWhiteSpace(TenantId)
            ? string.Empty
            : $"https://login.microsoftonline.com/{TenantId}/v2.0";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(TenantId) && !string.IsNullOrWhiteSpace(Audience);
}
