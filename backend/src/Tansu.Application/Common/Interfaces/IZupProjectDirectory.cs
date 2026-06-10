namespace Tansu.Application.Common.Interfaces;

public sealed record ZupProjectDto(
    Guid ProjectOid,
    int? ZupId,
    string? Code,
    string? Name,
    string? Description,
    string? Address,
    double? Latitude,
    double? Longitude,
    string? CustomerName,
    string? ProjectManagerName,
    string? ContractType);

public interface IZupProjectDirectory
{
    Task<IReadOnlyList<ZupProjectDto>> ListAsync(CancellationToken ct);
}
