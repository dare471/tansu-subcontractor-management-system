namespace Tansu.Domain.Enums;

public static class BatchStatus
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Draft, Submitted };
}
