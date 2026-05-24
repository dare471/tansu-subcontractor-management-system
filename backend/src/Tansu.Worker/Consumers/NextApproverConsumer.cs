using MassTransit;
using Microsoft.Extensions.Options;
using Tansu.Contracts.Messages;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

namespace Tansu.Worker.Consumers;

public sealed class NextApproverConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<NextApproverNotificationMessage>
{
    public async Task Consume(ConsumeContext<NextApproverNotificationMessage> ctx)
    {
        var msg = ctx.Message;
        var model = new
        {
            ApproverName = msg.ApproverFullName,
            msg.EmployeeFullName,
            msg.SubcontractorName,
            msg.OrderNo,
            InboxUrl = $"{links.Value.WebBaseUrl}/approvals/inbox"
        };

        var html = await renderer.RenderAsync("next-approver.cshtml", model);
        await sender.SendAsync(new EmailMessage(
            msg.ApproverEmail, msg.ApproverFullName,
            $"Согласование сотрудника: {msg.EmployeeFullName}", html), ctx.CancellationToken);
    }
}
