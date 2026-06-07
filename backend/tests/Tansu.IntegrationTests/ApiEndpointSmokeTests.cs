using FluentAssertions;
using Xunit;

namespace Tansu.IntegrationTests;

/// <summary>
/// Smoke-тест: каждый эндпоинт из <see cref="ApiEndpointCatalog"/> отвечает без ошибки 5xx.
/// </summary>
[Collection("Api")]
public class ApiEndpointSmokeTests(ApiFactory factory)
{
    private readonly ApiTestContext _ctx = new(factory);

    public static TheoryData<ApiEndpoint> AllEndpoints =>
        new(ApiEndpointCatalog.All);

    [Theory]
    [MemberData(nameof(AllEndpoints))]
    public async Task Endpoint_responds_without_server_error(ApiEndpoint endpoint)
    {
        var response = await _ctx.SendAsync(endpoint);
        ((int)response.StatusCode).Should().BeLessThan(500, $"endpoint {endpoint.Id} returned {(int)response.StatusCode}");
    }
}
