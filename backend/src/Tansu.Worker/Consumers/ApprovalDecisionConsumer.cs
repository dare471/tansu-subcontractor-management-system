using MassTransit;
using Microsoft.Extensions.Options;
using Tansu.Contracts.Messages;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

namespace Tansu.Worker.Consumers;

public sealed class ApprovalDecisionConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<EmployeeApprovalDecisionMessage>
{
    public async Task Consume(ConsumeContext<EmployeeApprovalDecisionMessage> ctx)
    {
        var msg = ctx.Message;
        var isApproved = msg.Decision == "approved";
        var subject = isApproved
            ? $"Согласовано: сотрудник {msg.EmployeeFullName}"
            : $"Отклонено: сотрудник {msg.EmployeeFullName}";

        var model = new
        {
            msg.EmployeeFullName,
            msg.SubcontractorName,
            msg.ApproverFullName,
            msg.Comment,
            IsApproved = isApproved,
            HistoryUrl = $"{links.Value.WebBaseUrl}/employees/{msg.EmployeeId}/approvals"
        };

        var html = await renderer.RenderAsync("approval-decision.cshtml", model);

        await sender.SendAsync(new EmailMessage(
            msg.ApproverEmail, msg.ApproverFullName, $"[Копия] {subject}", html), ctx.CancellationToken);

        if (!string.IsNullOrWhiteSpace(msg.InitiatorEmail) &&
            !msg.InitiatorEmail.Equals(msg.ApproverEmail, StringComparison.OrdinalIgnoreCase))
        {
            await sender.SendAsync(new EmailMessage(
                msg.InitiatorEmail, null, subject, html), ctx.CancellationToken);
        }
    }
}
