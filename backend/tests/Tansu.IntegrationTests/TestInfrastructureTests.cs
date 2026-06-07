using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Tansu.IntegrationTests;

[Collection("ApiScenario")]
public sealed class TestInfrastructureTests(ApiFactory factory)
{
    [Fact]
    public void Messaging_uses_in_memory_transport()
    {
        using var scope = factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();
        bus.Address.Scheme.Should().Be("loopback",
            $"integration tests must use in-memory MassTransit, got {bus.Address}");
    }
}
