namespace Tansu.Application.Common.Interfaces;

public sealed record ZupEmployeeDto(
    string ExternalId,
    string FullName,
    string Position,
    string Email,
    string Department = "",
    string Mobile = "");

public interface IZupEmployeeDirectory
{
    Task<IReadOnlyList<ZupEmployeeDto>> ListAsync(string employerCompany, CancellationToken ct);
}
