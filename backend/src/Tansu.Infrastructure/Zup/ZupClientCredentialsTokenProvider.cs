using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tansu.Infrastructure.Zup;

public sealed class ZupClientCredentialsTokenProvider(
    HttpClient http,
    IOptions<ZupOptions> options,
    ILogger<ZupClientCredentialsTokenProvider> logger) : IZupAccessTokenProvider
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var opts = options.Value;
        if (!opts.IsAuthConfigured)
            return null;

        var scope = opts.ResolveScope();
        if (string.IsNullOrEmpty(scope))
        {
            logger.LogWarning("ZUP: заданы ClientId/Secret, но не Scope и не Audience.");
            return null;
        }

        if (_cachedToken is not null && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
                return _cachedToken;

            var tokenUrl =
                $"https://login.microsoftonline.com/{opts.TenantId.Trim()}/oauth2/v2.0/token";

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = opts.ClientId.Trim(),
                ["client_secret"] = opts.ClientSecret,
                ["scope"] = scope
            });

            using var response = await http.PostAsync(tokenUrl, content, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ZUP token endpoint returned {Status}", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
            if (string.IsNullOrWhiteSpace(body?.AccessToken))
            {
                logger.LogWarning("ZUP token response has no access_token.");
                return null;
            }

            _cachedToken = body.AccessToken;
            var lifetime = body.ExpiresIn > 120 ? body.ExpiresIn - 60 : body.ExpiresIn;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(lifetime, 60));
            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
