using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Tansu.IntegrationTests;

[CollectionDefinition("Api", DisableParallelization = true)]
public sealed class ApiTestCollection : ICollectionFixture<ApiFactory>;
