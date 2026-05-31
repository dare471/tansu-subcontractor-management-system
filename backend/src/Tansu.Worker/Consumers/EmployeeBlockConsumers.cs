using MassTransit;
using Microsoft.Extensions.Options;
using Tansu.Contracts.Messages;
using Tansu.Domain.Enums;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

namespace Tansu.Worker.Consumers;

public sealed class EmployeeBlockedConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<EmployeeBlockedMessage>
{
    public async Task Consume(ConsumeContext<EmployeeBlockedMessage> ctx)
    {
        var msg = ctx.Message;
        if (msg.NotifyEmails.Count == 0)
            return;

        var model = new
        {
            msg.EmployeeFullName,
            msg.SubcontractorName,
            ProjectName = msg.ProjectName ?? "—",
            msg.InitiatorFullName,
            InitiatorRole = ApproverRole.Label(msg.InitiatorRole),
            msg.Reason,
            CardUrl = $"{links.Value.WebBaseUrl}/employees/{msg.EmployeeId}/approvals"
        };

        var html = await renderer.RenderAsync("employee-blocked.cshtml", model);
        foreach (var email in msg.NotifyEmails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await sender.SendAsync(new EmailMessage(
                email,
                null,
                $"Сотрудник {msg.EmployeeFullName} заблокирован",
                html), ctx.CancellationToken);
        }
    }
}

public sealed class EmployeeDocumentExpiringConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<EmployeeDocumentExpiringMessage>
{
    public async Task Consume(ConsumeContext<EmployeeDocumentExpiringMessage> ctx)
    {
        var msg = ctx.Message;
        if (msg.NotifyEmails.Count == 0)
            return;

        var model = new
        {
            msg.EmployeeFullName,
            msg.SubcontractorName,
            msg.DocumentName,
            DocumentType = EmployeeDocumentType.Label(msg.DocumentType),
            ExpiresAt = msg.ExpiresAt.ToString("dd.MM.yyyy"),
            CardUrl = $"{links.Value.WebBaseUrl}/employees/{msg.EmployeeId}/approvals"
        };

        var html = await renderer.RenderAsync("employee-document-expiring.cshtml", model);
        foreach (var email in msg.NotifyEmails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await sender.SendAsync(new EmailMessage(
                email,
                null,
                $"Истекает документ сотрудника {msg.EmployeeFullName}",
                html), ctx.CancellationToken);
        }
    }
}
