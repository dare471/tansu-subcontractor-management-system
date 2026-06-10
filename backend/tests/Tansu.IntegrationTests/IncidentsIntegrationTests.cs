using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

[Collection("ApiScenario")]
public sealed class IncidentsIntegrationTests(ApiFactory factory)
{
    [Fact]
    public async Task CreateIncident_lists_and_resolves_with_audit()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);

        var create = await http.PostAsJsonAsync("/api/incidents", new
        {
            projectOid = DemoSeedData.ProjectKeremetOid,
            occurredAt = DateTimeOffset.UtcNow,
            title = "Тестовый инцидент",
            description = "Описание для интеграционного теста",
            severity = "medium",
            blockUntilResolved = false,
            employeeIds = Array.Empty<Guid>()
        });
        create.EnsureSuccessStatusCode();
        var incident = await create.Content.ReadFromJsonAsync<IncidentDto>();
        incident.Should().NotBeNull();
        incident!.Status.Should().Be("open");

        var list = await http.GetAsync($"/api/incidents?projectOid={DemoSeedData.ProjectKeremetOid}");
        list.EnsureSuccessStatusCode();
        var items = await list.Content.ReadFromJsonAsync<IncidentDto[]>();
        items.Should().Contain(i => i.Id == incident.Id);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ITansuDbContext>();
        var createdAudit = await db.AuditEvents.AsNoTracking()
            .AnyAsync(e => e.Action == AuditActions.IncidentCreated && e.EntityId == incident.Id);
        createdAudit.Should().BeTrue();

        using var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/incidents/{incident.Id}")
        {
            Content = JsonContent.Create(new
            {
                status = "resolved",
                resolutionNotes = "Устранено в тесте"
            })
        };
        var patch = await http.SendAsync(patchRequest);
        patch.EnsureSuccessStatusCode();
        var resolved = await patch.Content.ReadFromJsonAsync<IncidentDto>();
        resolved!.Status.Should().Be("resolved");
        resolved.ResolvedAt.Should().NotBeNull();

        var resolvedAudit = await db.AuditEvents.AsNoTracking()
            .AnyAsync(e => e.Action == AuditActions.IncidentResolved && e.EntityId == incident.Id);
        resolvedAudit.Should().BeTrue();
    }

    private sealed record IncidentDto(
        Guid Id,
        string Status,
        DateTimeOffset? ResolvedAt);
}
