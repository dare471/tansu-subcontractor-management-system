namespace Tansu.Infrastructure.Zup;

public sealed class ZupOptions
{
    public const string SectionName = "Zup";

    public string BaseUrl { get; set; } = "https://api.tnsu.kz";
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;

    public bool IsAuthConfigured =>
        !string.IsNullOrWhiteSpace(TenantId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);

    public string ResolveScope()
    {
        if (!string.IsNullOrWhiteSpace(Scope))
            return Scope.Trim();

        var audience = Audience.Trim();
        if (string.IsNullOrEmpty(audience))
            return string.Empty;

        return audience.EndsWith("/.default", StringComparison.Ordinal)
            ? audience
            : $"{audience.TrimEnd('/')}/.default";
    }
}
