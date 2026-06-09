using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Auth;
using Tansu.Infrastructure.Persistence;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

[Collection("ApiScenario")]
public sealed class DelegationSlaIntegrationTests(ApiFactory factory)
{
    [Fact]
    public async Task CreateDelegation_lists_and_revokes_with_audit()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var delegateUser = await db.Users.AsNoTracking()
            .Where(u => u.UserType == UserType.Tansu && u.Email != DemoSeedData.TansuAdminEmail)
            .FirstAsync();

        var create = await http.PostAsJsonAsync("/api/delegations", new
        {
            delegateUserId = delegateUser.Id,
            validFrom = DateTimeOffset.UtcNow.AddDays(-1),
            validTo = DateTimeOffset.UtcNow.AddDays(14)
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<DelegationDto>();
        created.Should().NotBeNull();
        created!.IsActive.Should().BeTrue();

        var list = await http.GetAsync("/api/delegations?activeOnly=true");
        list.EnsureSuccessStatusCode();
        var items = await list.Content.ReadFromJsonAsync<DelegationDto[]>();
        items.Should().Contain(d => d.Id == created.Id);

        var auditDb = scope.ServiceProvider.GetRequiredService<ITansuDbContext>();
        var createdAudit = await auditDb.AuditEvents.AsNoTracking()
            .AnyAsync(e => e.Action == AuditActions.DelegationCreated && e.EntityId == created.Id);
        createdAudit.Should().BeTrue();

        var revoke = await http.DeleteAsync($"/api/delegations/{created.Id}");
        revoke.EnsureSuccessStatusCode();

        var revokedAudit = await auditDb.AuditEvents.AsNoTracking()
            .AnyAsync(e => e.Action == AuditActions.DelegationRevoked && e.EntityId == created.Id);
        revokedAudit.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitEmployee_applies_delegation_to_approval_sheet()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var sub = await db.Subcontractors.FirstAsync();
        var delegator = await db.Users.FirstAsync(u => u.Email == DemoSeedData.TansuApprovers[0].Email);
        var delegateUser = await db.Users.FirstAsync(u => u.Email == DemoSeedData.TansuApprovers[1].Email);

        db.ApproverDelegations.Add(new ApproverDelegation
        {
            DelegatorUserId = delegator.Id,
            DelegateUserId = delegateUser.Id,
            ValidFrom = DateTimeOffset.UtcNow.AddDays(-1),
            ValidTo = DateTimeOffset.UtcNow.AddDays(30),
            CreatedByUserId = delegator.Id
        });

        var oldMatrix = await db.ApprovalMatrix
            .Where(m => m.SubcontractorId == sub.Id && m.ProjectOid == DemoSeeder.DemoProjectOid)
            .ToListAsync();
        db.ApprovalMatrix.RemoveRange(oldMatrix);
        db.ApprovalMatrix.Add(new ApprovalMatrixEntry
        {
            OrderNo = 1,
            ProjectOid = DemoSeeder.DemoProjectOid,
            SubcontractorId = sub.Id,
            UserId = delegator.Id
        });

        var initiator = new User
        {
            FullName = "Делегирование тест",
            Position = "HR",
            Email = $"deleg-{Guid.NewGuid():N}@example.kz",
            UserType = UserType.Subcontractor,
            SubcontractorId = sub.Id,
            PasswordHash = hasher.Hash(DemoSeedData.SubcontractorTempPassword),
            MustChangePassword = false,
            IsActive = true
        };
        db.Users.Add(initiator);

        var employee = new Employee
        {
            SubcontractorId = sub.Id,
            ProjectOid = DemoSeeder.DemoProjectOid,
            FullName = "Тест делегирования",
            Position = "Монтажник",
            Phone = "+7 771 000 0001",
            Iin = $"9{Guid.NewGuid():N}"[..12],
            PhotoPath = "demo/delegation.jpg",
            PhotoReviewStatus = EmployeePhotoReviewStatus.Approved
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var subHttp = factory.CreateClient();
        var token = jwt.IssueLocalToken(initiator).AccessToken;
        subHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var submit = await subHttp.PostAsync($"/api/employees/{employee.Id}/submit", null);
        submit.EnsureSuccessStatusCode();

        var sheet = await db.ApprovalSheet.AsNoTracking()
            .Where(s => s.EmployeeId == employee.Id && s.Status == ApprovalStatus.Pending)
            .OrderBy(s => s.OrderNo)
            .FirstAsync();

        sheet.ApproverUserId.Should().Be(delegateUser.Id);
        sheet.ActingForUserId.Should().Be(delegator.Id);
        sheet.AssignedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Global_sla_policy_is_seeded()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TansuDbContext>();

        var policy = await db.ApprovalSlaPolicies.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Scope == "global" && p.IsActive);

        policy.Should().NotBeNull();
        policy!.PendingDaysWarning.Should().BeGreaterThan(0);
        policy.PendingDaysEscalation.Should().BeGreaterThan(policy.PendingDaysWarning);
    }

    [Fact]
    public async Task Inbox_returns_sla_fields_for_pending_items()
    {
        var http = await IntegrationTestAuth.LoginAdminAsync(factory);
        var res = await http.GetAsync("/api/approvals/inbox");
        res.EnsureSuccessStatusCode();

        var inbox = await res.Content.ReadFromJsonAsync<InboxItemDto[]>();
        if (inbox is null || inbox.Length == 0)
            return;

        inbox[0].PendingDays.Should().NotBeNull();
        inbox[0].CanAct.Should().BeTrue();
    }

    private sealed record DelegationDto(
        Guid Id,
        Guid DelegateUserId,
        bool IsActive);
    private sealed record InboxItemDto(
        int? PendingDays,
        bool IsEscalated,
        string? ActingForApproverName,
        bool CanAct);
}
