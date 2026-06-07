using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Infrastructure.Zup;

public sealed class HttpZupEmployeeDirectory(
    HttpClient http,
    IOptions<ZupOptions> options,
    IZupAccessTokenProvider tokenProvider,
    ILogger<HttpZupEmployeeDirectory> logger) : IZupEmployeeDirectory
{
    public async Task<IReadOnlyList<ZupEmployeeDto>> ListAsync(string employerCompany, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Value.BaseUrl))
            return Array.Empty<ZupEmployeeDto>();

        var company = TansuEmployerCompany.ZupQueryValue(employerCompany);
        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/Employee/All?company={Uri.EscapeDataString(company)}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var token = await tokenProvider.GetAccessTokenAsync(ct);
            if (token is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            else if (options.Value.IsAuthConfigured)
            {
                logger.LogWarning("ZUP: не удалось получить access token.");
                return Array.Empty<ZupEmployeeDto>();
            }

            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ZUP API {Url} returned {Status}", url, response.StatusCode);
                return Array.Empty<ZupEmployeeDto>();
            }

            var rows = await response.Content.ReadFromJsonAsync<List<ZupEmployeeApiRow>>(ct);
            if (rows is null || rows.Count == 0)
                return Array.Empty<ZupEmployeeDto>();

            return rows
                .Select(r => new ZupEmployeeDto(
                    (r.Id ?? r.TabNumber ?? r.Email ?? r.ResolveFullName() ?? Guid.NewGuid().ToString())
                        .ToString()!,
                    r.ResolveFullName(),
                    (r.Position ?? r.JobTitle ?? string.Empty).Trim(),
                    (r.Email ?? string.Empty).Trim().ToLowerInvariant(),
                    (r.Department ?? string.Empty).Trim(),
                    NormalizeMobile(r.Mobile)))
                .Where(x => !string.IsNullOrWhiteSpace(x.FullName))
                .DistinctBy(x => x.Email.Length > 0 ? x.Email : x.FullName)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ZUP API request failed for {Company}", company);
            return Array.Empty<ZupEmployeeDto>();
        }
    }

    private static string NormalizeMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
            return string.Empty;

        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        return digits.Length > 0 ? digits : mobile.Trim();
    }

    private sealed class ZupEmployeeApiRow
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("tabNumber")]
        public string? TabNumber { get; set; }

        [JsonPropertyName("fio")]
        public string? Fio { get; set; }

        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("position")]
        public string? Position { get; set; }

        [JsonPropertyName("jobTitle")]
        public string? JobTitle { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("department")]
        public string? Department { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        public string ResolveFullName() =>
            (Fio ?? FullName ?? Name ?? string.Empty).Trim();
    }
}
