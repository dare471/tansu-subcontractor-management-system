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

public class ApprovalFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory = factory;

    [Fact]
    public async Task Reject_marks_downstream_steps_as_skipped()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var sub = await db.Subcontractors.FirstAsync();
        var initiator = await db.Users.FirstAsync(u =>
            u.UserType == UserType.Subcontractor && u.SubcontractorId == sub.Id);

        var approver1 = new User
        {
            FullName = "Берик Оспанов", Position = "Руководитель проекта",
            Email = $"a1-{Guid.NewGuid():N}@tansu.local", UserType = UserType.Tansu,
            PasswordHash = hasher.Hash(DemoSeedData.SubcontractorTempPassword), MustChangePassword = false
        };
        var approver2 = new User
        {
            FullName = "Алия Нуржанова", Position = "Инженер по ОТ и ТБ",
            Email = $"a2-{Guid.NewGuid():N}@tansu.local", UserType = UserType.Tansu,
            PasswordHash = hasher.Hash(DemoSeedData.SubcontractorTempPassword), MustChangePassword = false
        };
        db.Users.Add(approver1);
        db.Users.Add(approver2);

        var oldMatrix = await db.ApprovalMatrix
            .Where(m => m.SubcontractorId == sub.Id && m.ProjectOid == DemoSeeder.DemoProjectOid)
            .ToListAsync();
        db.ApprovalMatrix.RemoveRange(oldMatrix);

        db.ApprovalMatrix.Add(new ApprovalMatrixEntry
        {
            OrderNo = 1, ProjectOid = DemoSeeder.DemoProjectOid,
            SubcontractorId = sub.Id, UserId = approver1.Id
        });
        db.ApprovalMatrix.Add(new ApprovalMatrixEntry
        {
            OrderNo = 2, ProjectOid = DemoSeeder.DemoProjectOid,
            SubcontractorId = sub.Id, UserId = approver2.Id
        });

        var employee = new Employee
        {
            SubcontractorId = sub.Id, ProjectOid = DemoSeeder.DemoProjectOid,
            FullName = "Айдос Нұрланов", Position = "Монтажник", Phone = "+7 771 482 9156",
            Iin = "880512301456"
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        initiator.MustChangePassword = false;
        await db.SaveChangesAsync();
        var initiatorToken = jwt.IssueLocalToken(initiator).AccessToken;

        var http = _factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", initiatorToken);

        var submit = await http.PostAsync($"/api/employees/{employee.Id}/submit", content: null);
        submit.EnsureSuccessStatusCode();

        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => a.EmployeeId == employee.Id)
            .OrderBy(a => a.OrderNo).ToListAsync();
        sheets.Should().HaveCount(2);
        sheets.All(s => s.Status == ApprovalStatus.Pending).Should().BeTrue();

        var approver1Token = jwt.IssueLocalToken(new User
        {
            Id = approver1.Id, Email = approver1.Email, UserType = UserType.Tansu,
            FullName = approver1.FullName, MustChangePassword = false
        }).AccessToken;

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", approver1Token);
        var firstSheet = sheets.First(s => s.OrderNo == 1);
        var reject = await http.PostAsJsonAsync(
            $"/api/approvals/{firstSheet.Id}/reject", new { comment = "Документы оформлены неверно" });
        reject.EnsureSuccessStatusCode();

        var after = await db.ApprovalSheet.AsNoTracking()
            .Where(a => a.EmployeeId == employee.Id)
            .OrderBy(a => a.OrderNo).ToListAsync();
        after.First(s => s.OrderNo == 1).Status.Should().Be(ApprovalStatus.Rejected);
        after.First(s => s.OrderNo == 2).Status.Should().Be(ApprovalStatus.Skipped);
    }
}
