namespace Tansu.Application.PpeIssuance;

public sealed record EmployeePpeIssuanceDto(
    Guid Id,
    Guid EmployeeId,
    string ItemType,
    string? Size,
    string? InventoryNumber,
    DateTimeOffset IssuedAt,
    string IssuedByFullName,
    DateTimeOffset? ReturnedAt,
    string? Notes,
    bool IsActive);

public sealed record EmployeePpeSummaryDto(
    bool HasHelmet,
    bool HasUniform,
    EmployeePpeIssuanceDto? ActiveHelmet,
    EmployeePpeIssuanceDto? ActiveUniform,
    IReadOnlyList<EmployeePpeIssuanceDto> History);

public sealed record IssueEmployeePpeRequest(
    string ItemType,
    string? Size,
    string? InventoryNumber,
    string? Notes);

public sealed record ReturnEmployeePpeRequest(string? Notes);
