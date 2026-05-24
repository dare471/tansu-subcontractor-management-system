using MassTransit;
using Microsoft.Extensions.Options;
using Tansu.Contracts.Messages;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

namespace Tansu.Worker.Consumers;

public sealed class ApprovalSubmittedConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<ApprovalSubmittedMessage>
{
    public async Task Consume(ConsumeContext<ApprovalSubmittedMessage> ctx)
    {
        var msg = ctx.Message;
        var model = new
        {
            msg.EmployeeFullName,
            msg.SubcontractorName,
            FirstApproverName = msg.FirstApproverFullName,
            HistoryUrl = $"{links.Value.WebBaseUrl}/employees/{msg.EmployeeId}/approvals"
        };

        var html = await renderer.RenderAsync("approval-submitted.cshtml", model);

        await sender.SendAsync(new EmailMessage(
            msg.InitiatorEmail, null,
            $"Сотрудник {msg.EmployeeFullName} отправлен на согласование", html), ctx.CancellationToken);
    }
}
