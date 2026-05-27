namespace Tansu.Domain.Enums;

public static class UserType
{
    public const string Tansu = "TANSU";
    public const string Subcontractor = "Subcontractor";
    public const string Employee = "Employee";

    public static bool IsValid(string? value) =>
        value is Tansu or Subcontractor or Employee;
}
