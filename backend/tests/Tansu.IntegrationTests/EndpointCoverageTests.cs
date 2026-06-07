using FluentAssertions;
using Xunit;

namespace Tansu.IntegrationTests;

/// <summary>
/// Meta-тест: каталог эндпоинтов синхронизирован с набором smoke-тестов.
/// </summary>
public class EndpointCoverageTests
{
    [Fact]
    public void Catalog_has_expected_endpoint_count()
    {
        ApiEndpointCatalog.All.Should().HaveCount(ApiEndpointCatalog.ExpectedCount);
    }

    [Fact]
    public void Catalog_ids_are_unique()
    {
        var ids = ApiEndpointCatalog.All.Select(e => e.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Smoke_tests_cover_every_catalog_endpoint()
    {
        var catalogIds = ApiEndpointCatalog.All.Select(e => e.Id).OrderBy(x => x).ToList();
        ((IEnumerable<ApiEndpoint>)ApiEndpointSmokeTests.AllEndpoints)
            .Count()
            .Should()
            .Be(catalogIds.Count, "smoke-тесты должны покрывать каждый эндпоинт из ApiEndpointCatalog");
    }
}
