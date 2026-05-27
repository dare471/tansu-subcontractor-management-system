namespace Tansu.Application.AccessPasses;

public class AccessPassOptions
{
    public const string SectionName = "AccessPass";

    public string VerifyWebBaseUrl { get; set; } = "http://localhost:5174";
    public string VerifyServiceKey { get; set; } = "dev-verify-service-key-change-me";
}
