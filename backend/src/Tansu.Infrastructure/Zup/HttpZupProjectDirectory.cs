using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Infrastructure.Zup;

public sealed class HttpZupProjectDirectory(
    HttpClient http,
    IOptions<ZupOptions> options,
    IZupAccessTokenProvider tokenProvider,
    ILogger<HttpZupProjectDirectory> logger) : IZupProjectDirectory
{
    private const int PageSize = 100;

    public async Task<IReadOnlyList<ZupProjectDto>> ListAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Value.BaseUrl))
            return Array.Empty<ZupProjectDto>();

        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        var token = await tokenProvider.GetAccessTokenAsync(ct);
        if (token is null && options.Value.IsAuthConfigured)
        {
            logger.LogWarning("ZUP: не удалось получить access token для /Project.");
            return Array.Empty<ZupProjectDto>();
        }

        try
        {
            var all = new List<ZupProjectDto>();
            var page = 1;
            int totalPages;

            do
            {
                var url = $"{baseUrl}/Project?page={page}&pageSize={PageSize}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (token is not null)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await http.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("ZUP API {Url} returned {Status}", url, response.StatusCode);
                    break;
                }

                var body = await response.Content.ReadFromJsonAsync<ZupProjectPageResponse>(ct);
                if (body?.Items is null || body.Items.Count == 0)
                    break;

                foreach (var row in body.Items)
                {
                    if (row.Deleted)
                        continue;
                    if (row.ToDto() is { } dto)
                        all.Add(dto);
                }

                totalPages = body.TotalPages > 0 ? body.TotalPages : 1;
                page++;
            } while (page <= totalPages);

            return all.DistinctBy(x => x.ProjectOid).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ZUP API /Project request failed");
            return Array.Empty<ZupProjectDto>();
        }
    }

    private sealed class ZupProjectPageResponse
    {
        [JsonPropertyName("items")]
        public List<ZupProjectApiRow>? Items { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }
    }

    private sealed class ZupProjectApiRow
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("oid")]
        public string? Oid { get; set; }

        [JsonPropertyName("projectOid")]
        public string? ProjectOid { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("projectManager")]
        public string? ProjectManager { get; set; }

        [JsonPropertyName("contractType")]
        public string? ContractType { get; set; }

        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }

        public ZupProjectDto? ToDto()
        {
            var oidRaw = ProjectOid ?? Oid;
            if (!Guid.TryParse(oidRaw, out var oid))
                return null;

            var name = (Name ?? string.Empty).Trim();
            return new ZupProjectDto(
                oid,
                Id,
                NullIfEmpty(Code),
                NullIfEmpty(name),
                NullIfEmpty(Description),
                NullIfEmpty(Address),
                Latitude,
                Longitude,
                NullIfEmpty(CustomerName),
                NullIfEmpty(ProjectManager),
                NullIfEmpty(ContractType));
        }

        private static string? NullIfEmpty(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();
        }
    }
}
