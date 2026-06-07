using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Tansu.IntegrationTests;

/// <summary>
/// Сценарные тесты (auth, users, approvals) — отдельный хост и БД, запускаются до smoke.
/// </summary>
[CollectionDefinition("ApiScenario", DisableParallelization = true)]
public sealed class ApiScenarioCollection : ICollectionFixture<ApiFactory>;

/// <summary>
/// Smoke по всем эндпоинтам — отдельный хост и БД, чтобы не влиять на сценарные тесты.
/// </summary>
[CollectionDefinition("ApiSmoke", DisableParallelization = true)]
public sealed class ApiSmokeCollection : ICollectionFixture<ApiFactory>;
