using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Tansu.Application.Common.Interfaces;
using Tansu.Infrastructure.AccessControl;

namespace Tansu.UnitTests;

public sealed class AccessControlOrchestratorTests
{
    [Fact]
    public async Task SyncPerson_calls_all_registered_adapters()
    {
        var hik = new RecordingAdapter("hik");
        var perco = new RecordingAdapter("perco");
        var orchestrator = new AccessControlOrchestrator(
            [hik, perco],
            NullLogger<AccessControlOrchestrator>.Instance);

        var person = new AccessControlPerson(Guid.NewGuid(), "Test User", null, null, null, null);
        await orchestrator.SyncPersonAsync(person, null, CancellationToken.None);

        hik.SyncCount.Should().Be(1);
        perco.SyncCount.Should().Be(1);
    }

    [Fact]
    public async Task RevokePerson_continues_when_one_adapter_throws()
    {
        var ok = new RecordingAdapter("ok");
        var failing = new FailingAdapter("fail");
        var orchestrator = new AccessControlOrchestrator(
            [failing, ok],
            NullLogger<AccessControlOrchestrator>.Instance);

        await orchestrator.Invoking(o =>
                o.RevokePersonAsync(Guid.NewGuid(), "reason", null, CancellationToken.None))
            .Should().NotThrowAsync();

        ok.RevokeCount.Should().Be(1);
    }

    private sealed class RecordingAdapter(string vendorId) : IAccessControlSystem
    {
        public string VendorId => vendorId;
        public int SyncCount { get; private set; }
        public int RevokeCount { get; private set; }

        public Task SyncPersonAsync(AccessControlPerson person, CancellationToken ct)
        {
            SyncCount++;
            return Task.CompletedTask;
        }

        public Task RevokePersonAsync(Guid employeeId, string reason, CancellationToken ct)
        {
            RevokeCount++;
            return Task.CompletedTask;
        }

        public Task<bool> IsHealthyAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FailingAdapter(string vendorId) : IAccessControlSystem
    {
        public string VendorId => vendorId;

        public Task SyncPersonAsync(AccessControlPerson person, CancellationToken ct) =>
            throw new InvalidOperationException("sync failed");

        public Task RevokePersonAsync(Guid employeeId, string reason, CancellationToken ct) =>
            throw new InvalidOperationException("revoke failed");

        public Task<bool> IsHealthyAsync(CancellationToken ct) => Task.FromResult(false);
    }
}
