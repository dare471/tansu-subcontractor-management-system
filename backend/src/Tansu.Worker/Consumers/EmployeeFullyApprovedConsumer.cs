using MassTransit;
using Microsoft.Extensions.Options;
using Tansu.Contracts.Messages;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

namespace Tansu.Worker.Consumers;

public sealed class EmployeeFullyApprovedConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<EmployeeFullyApprovedMessage>
{
    public async Task Consume(ConsumeContext<EmployeeFullyApprovedMessage> ctx)
    {
        var msg = ctx.Message;
        var model = new
        {
            msg.EmployeeFullName,
            msg.SubcontractorName,
            HistoryUrl = $"{links.Value.WebBaseUrl}/employees/{msg.EmployeeId}/approvals"
        };

        var html = await renderer.RenderAsync("approval-fully-approved.cshtml", model);
        await sender.SendAsync(new EmailMessage(
            msg.InitiatorEmail, null,
            $"Сотрудник {msg.EmployeeFullName} полностью согласован", html), ctx.CancellationToken);
    }
}
