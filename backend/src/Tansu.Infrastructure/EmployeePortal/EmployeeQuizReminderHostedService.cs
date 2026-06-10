using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tansu.Application.Common.Interfaces;
using Tansu.Contracts.Messages;
using Tansu.Domain.Enums;

namespace Tansu.Infrastructure.EmployeePortal;

public sealed class EmployeeQuizReminderHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<EmployeeQuizReminderHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Quiz reminder job failed");
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ITansuDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var cutoff24 = DateTimeOffset.UtcNow.AddHours(-24);
        var cutoff72 = DateTimeOffset.UtcNow.AddHours(-72);

        var portalUsers = await db.Users.AsNoTracking()
            .Include(u => u.Employee!).ThenInclude(e => e!.Subcontractor)
            .Where(u => u.IsActive &&
                        u.UserType == UserType.Employee &&
                        u.EmployeeId != null)
            .ToListAsync(ct);

        var completed = await db.EmployeeSafetyQuizCompletions.AsNoTracking()
            .Select(q => q.EmployeeId)
            .ToListAsync(ct);
        var completedSet = completed.ToHashSet();

        foreach (var user in portalUsers)
        {
            var emp = user.Employee!;
            if (completedSet.Contains(emp.Id)) continue;

            var email = user.NotificationEmail ?? user.Email;
            if (string.IsNullOrWhiteSpace(email)) continue;

            var needsReminder = user.CreatedAt <= cutoff24;
            if (!needsReminder) continue;

            await publisher.Publish(new EmployeeQuizReminderMessage(
                emp.Id, emp.FullName, email,
                emp.SubcontractorId, emp.Subcontractor?.Name ?? "—",
                DateTimeOffset.UtcNow), ct);
        }
    }
}
