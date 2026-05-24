namespace Tansu.Domain.Enums;

public static class DocumentRequestType
{
    public const string Leave = "leave";
    public const string Ticket = "ticket";
    public const string Document = "document";
    public const string Expense = "expense";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Leave, Ticket, Document, Expense
    };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
