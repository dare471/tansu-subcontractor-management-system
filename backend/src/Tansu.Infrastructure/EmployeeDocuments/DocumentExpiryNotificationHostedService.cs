using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tansu.Application.Common.Interfaces;
using Tansu.Contracts.Messages;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.EmployeeDocuments;

public sealed class DocumentExpiryNotificationHostedService(
    IServiceProvider services,
    ILogger<DocumentExpiryNotificationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Document expiry notification check failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CheckAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var now = DateTimeOffset.UtcNow;
        var windowEnd = now.AddDays(14);

        var docs = await db.EmployeeDocuments
            .Include(d => d.Employee!)
            .ThenInclude(e => e!.Subcontractor)
            .Where(d =>
                d.ExpiresAt != null &&
                d.ExpiresAt > now &&
                d.ExpiresAt <= windowEnd &&
                d.ExpiryNotifiedAt == null)
            .ToListAsync(ct);

        if (docs.Count == 0)
            return;

        foreach (var doc in docs)
        {
            var employee = doc.Employee!;
            var emails = await ResolveNotifyEmailsAsync(db, employee.SubcontractorId, ct);
            if (emails.Count == 0)
                continue;

            await publisher.Publish(new EmployeeDocumentExpiringMessage(
                doc.Id,
                employee.Id,
                employee.FullName,
                doc.Name,
                doc.DocumentType,
                doc.ExpiresAt!.Value,
                employee.SubcontractorId,
                employee.Subcontractor?.Name ?? "—",
                emails), ct);

            doc.ExpiryNotifiedAt = now;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Published {Count} document expiry notifications", docs.Count);
    }

    private static async Task<IReadOnlyList<string>> ResolveNotifyEmailsAsync(
        TansuDbContext db,
        Guid subcontractorId,
        CancellationToken ct)
    {
        var subcontractorEmails = await db.Users.AsNoTracking()
            .Where(u => u.IsActive &&
                        u.UserType == UserType.Subcontractor &&
                        u.SubcontractorId == subcontractorId)
            .Select(u => u.Email)
            .ToListAsync(ct);

        var tansuEmails = await db.Users.AsNoTracking()
            .Where(u => u.IsActive &&
                        u.UserType == UserType.Tansu &&
                        (u.ApproverRole == ApproverRole.Safety ||
                         u.ApproverRole == ApproverRole.OID ||
                         u.IsSuperUser))
            .Select(u => u.Email)
            .ToListAsync(ct);

        return subcontractorEmails
            .Concat(tansuEmails)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
