using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tansu.Application.Common.Interfaces;
using Tansu.Contracts.Messages;
using Tansu.Domain.Enums;

namespace Tansu.Infrastructure.Approvals;

public sealed class ApprovalSlaMonitorHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ApprovalSlaMonitorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SLA monitor failed");
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ITansuDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var policy = await db.ApprovalSlaPolicies.AsNoTracking()
            .Where(p => p.IsActive && p.Scope == "global")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? new Domain.Entities.ApprovalSlaPolicy();

        var now = DateTimeOffset.UtcNow;

        var empSheets = await db.ApprovalSheet
            .Include(s => s.Employee)
            .Include(s => s.Approver)
            .Where(s => s.Status == ApprovalStatus.Pending && s.AssignedAt != null)
            .ToListAsync(ct);

        foreach (var sheet in empSheets)
        {
            var days = (int)(now - sheet.AssignedAt!.Value).TotalDays;
            if (days >= policy.PendingDaysWarning &&
                (sheet.LastReminderAt == null || (now - sheet.LastReminderAt.Value).TotalDays >= 1))
            {
                await publisher.Publish(new ApprovalSlaWarningMessage(
                    "employee", sheet.Id, sheet.ApproverUserId,
                    sheet.Approver!.Email, sheet.Approver.FullName,
                    sheet.Employee?.FullName ?? "Сотрудник", days, now), ct);
                sheet.LastReminderAt = now;
            }
            if (days >= policy.PendingDaysEscalation && sheet.EscalatedAt == null)
            {
                var escalation = await db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IsActive && u.ApproverRole == policy.EscalationRole, ct);
                if (escalation != null)
                {
                    await publisher.Publish(new ApprovalEscalationMessage(
                        "employee", sheet.Id,
                        sheet.Employee?.FullName ?? "Сотрудник",
                        escalation.Email, escalation.FullName, days, now), ct);
                    sheet.EscalatedAt = now;
                }
            }
        }

        var docSheets = await db.DocumentApprovalSheet
            .Include(s => s.DocumentRequest)
            .Include(s => s.Approver)
            .Where(s => s.Status == ApprovalStatus.Pending && s.AssignedAt != null)
            .ToListAsync(ct);

        foreach (var sheet in docSheets)
        {
            var days = (int)(now - sheet.AssignedAt!.Value).TotalDays;
            if (days >= policy.PendingDaysWarning &&
                (sheet.LastReminderAt == null || (now - sheet.LastReminderAt.Value).TotalDays >= 1))
            {
                await publisher.Publish(new ApprovalSlaWarningMessage(
                    "document_request", sheet.Id, sheet.ApproverUserId,
                    sheet.Approver!.Email, sheet.Approver.FullName,
                    sheet.DocumentRequest?.Title ?? "Заявка", days, now), ct);
                sheet.LastReminderAt = now;
            }
            if (days >= policy.PendingDaysEscalation && sheet.EscalatedAt == null)
            {
                var escalation = await db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IsActive && u.ApproverRole == policy.EscalationRole, ct);
                if (escalation != null)
                {
                    await publisher.Publish(new ApprovalEscalationMessage(
                        "document_request", sheet.Id,
                        sheet.DocumentRequest?.Title ?? "Заявка",
                        escalation.Email, escalation.FullName, days, now), ct);
                    sheet.EscalatedAt = now;
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
