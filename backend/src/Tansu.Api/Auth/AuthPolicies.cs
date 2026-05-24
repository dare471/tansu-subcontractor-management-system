namespace Tansu.Api.Auth;

public static class AuthPolicies
{
    public const string TansuOnly = "TansuOnly";
    public const string SubcontractorOnly = "SubcontractorOnly";
}

public static class AuthSchemes
{
    public const string LocalJwt = "LocalJwt";
    public const string Entra = "Entra";
    public const string Both = LocalJwt + "," + Entra;
}
