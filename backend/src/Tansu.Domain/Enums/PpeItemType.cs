namespace Tansu.Domain.Enums;

public static class PpeItemType
{
    public const string Helmet = "helmet";
    public const string Uniform = "uniform";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Helmet,
        Uniform
    };
}
