using Tansu.Domain.Entities;

namespace Tansu.Application.PpeIssuance;

internal static class PpeIssuanceMapper
{
    public static EmployeePpeIssuanceDto ToDto(EmployeePpeIssuance row) =>
        new(
            row.Id,
            row.EmployeeId,
            row.ItemType,
            row.Size,
            row.InventoryNumber,
            row.IssuedAt,
            row.IssuedBy?.FullName ?? "—",
            row.ReturnedAt,
            row.Notes,
            row.ReturnedAt is null);

    public static EmployeePpeSummaryDto ToSummary(IReadOnlyList<EmployeePpeIssuance> rows)
    {
        var dtos = rows
            .OrderByDescending(r => r.IssuedAt)
            .Select(ToDto)
            .ToList();

        var activeHelmet = dtos.FirstOrDefault(d => d.ItemType == Domain.Enums.PpeItemType.Helmet && d.IsActive);
        var activeUniform = dtos.FirstOrDefault(d => d.ItemType == Domain.Enums.PpeItemType.Uniform && d.IsActive);

        return new EmployeePpeSummaryDto(
            activeHelmet is not null,
            activeUniform is not null,
            activeHelmet,
            activeUniform,
            dtos);
    }
}
