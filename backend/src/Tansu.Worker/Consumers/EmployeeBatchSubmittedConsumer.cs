using MassTransit;
using Microsoft.Extensions.Options;
using Tansu.Contracts.Messages;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

namespace Tansu.Worker.Consumers;

public sealed class EmployeeBatchSubmittedConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<EmployeeBatchSubmittedMessage>
{
    public async Task Consume(ConsumeContext<EmployeeBatchSubmittedMessage> ctx)
    {
        var msg = ctx.Message;
        var employeeLines = msg.Employees
            .Select(e => $"{e.FullName} — {e.Position}")
            .ToList();

        var approverModel = new
        {
            ApproverName = msg.FirstApproverFullName,
            msg.BatchTitle,
            msg.SubcontractorName,
            ProjectName = msg.ProjectName ?? msg.ProjectOid.ToString(),
            EmployeeCount = msg.Employees.Count,
            Employees = employeeLines,
            InboxUrl = $"{links.Value.WebBaseUrl}/approvals/inbox"
        };

        var approverHtml = await renderer.RenderAsync("employee-batch-submitted.cshtml", approverModel);
        await sender.SendAsync(new EmailMessage(
            msg.FirstApproverEmail, null,
            $"Пакет «{msg.BatchTitle}» — {msg.Employees.Count} чел.", approverHtml),
            ctx.CancellationToken);

        var initiatorModel = new
        {
            msg.BatchTitle,
            EmployeeCount = msg.Employees.Count,
            Employees = employeeLines,
            BatchesUrl = $"{links.Value.WebBaseUrl}/employee-batches"
        };

        var initiatorHtml = await renderer.RenderAsync("employee-batch-submitted-initiator.cshtml", initiatorModel);
        await sender.SendAsync(new EmailMessage(
            msg.InitiatorEmail, null,
            $"Пакет «{msg.BatchTitle}» отправлен на согласование", initiatorHtml),
            ctx.CancellationToken);
    }
}
