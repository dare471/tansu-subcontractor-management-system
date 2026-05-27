namespace Tansu.Application.EmployeePortal;

public sealed class EmployeePortalOptions
{
    public const string SectionName = "EmployeePortal";
    public string CredentialsLogPath { get; set; } = "data/employee-portal-credentials.log";
}
