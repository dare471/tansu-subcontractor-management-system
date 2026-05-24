using MassTransit;
using Microsoft.Extensions.Options;
using Tansu.Contracts.Messages;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

namespace Tansu.Worker.Consumers;

public sealed class DocumentRequestNextApproverConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<DocumentRequestNextApproverMessage>
{
    public async Task Consume(ConsumeContext<DocumentRequestNextApproverMessage> ctx)
    {
        var msg = ctx.Message;
        var model = new
        {
            ApproverName = msg.ApproverFullName,
            msg.Title,
            TypeLabel = RequestTypeLabel(msg.RequestType),
            RoleLabel = RoleLabel(msg.ApproverRole),
            msg.SubcontractorName,
            msg.OrderNo,
            InboxUrl = $"{links.Value.WebBaseUrl}/document-requests/inbox"
        };

        var html = await renderer.RenderAsync("document-request-next-approver.cshtml", model);
        await sender.SendAsync(new EmailMessage(
            msg.ApproverEmail, msg.ApproverFullName,
            $"Заявка «{msg.Title}» — согласование ({model.TypeLabel})", html), ctx.CancellationToken);
    }

    private static string RequestTypeLabel(string type) => type switch
    {
        "leave" => "Отпуск",
        "ticket" => "Тикет",
        "document" => "Документ",
        "expense" => "Расход",
        _ => type
    };

    private static string RoleLabel(string role) => role switch
    {
        "accounting" => "Бухгалтерия",
        "hr" => "Кадры",
        "finance" => "Финансы",
        "management" => "Руководство",
        _ => role
    };
}

public sealed class DocumentRequestSubmittedConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<DocumentRequestSubmittedMessage>
{
    public async Task Consume(ConsumeContext<DocumentRequestSubmittedMessage> ctx)
    {
        var msg = ctx.Message;
        var model = new
        {
            msg.Title,
            TypeLabel = msg.RequestType switch
            {
                "leave" => "Отпуск",
                "ticket" => "Тикет",
                "document" => "Документ",
                "expense" => "Расход",
                _ => msg.RequestType
            },
            msg.SubcontractorName,
            RequestsUrl = $"{links.Value.WebBaseUrl}/document-requests"
        };

        var html = await renderer.RenderAsync("document-request-submitted.cshtml", model);
        await sender.SendAsync(new EmailMessage(
            msg.InitiatorEmail, null,
            $"Заявка «{msg.Title}» отправлена на согласование", html), ctx.CancellationToken);
    }
}
