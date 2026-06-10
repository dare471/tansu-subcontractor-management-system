using MassTransit;
using Microsoft.Extensions.Options;
using Tansu.Contracts.Messages;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

namespace Tansu.Worker.Consumers;

public sealed class DocumentRequestDecisionConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<DocumentRequestDecisionMessage>
{
    public async Task Consume(ConsumeContext<DocumentRequestDecisionMessage> ctx)
    {
        var msg = ctx.Message;
        var decisionLabel = msg.Decision == "approved" ? "согласована" : "отклонена";
        var model = new
        {
            msg.Title,
            DecisionLabel = decisionLabel,
            msg.Comment,
            msg.SubcontractorName,
            ApproverName = msg.ApproverFullName,
            RequestsUrl = $"{links.Value.WebBaseUrl}/document-requests"
        };
        var html = await renderer.RenderAsync("document-request-decision.cshtml", model);
        await sender.SendAsync(new EmailMessage(
            msg.InitiatorEmail, null,
            $"Заявка «{msg.Title}» {decisionLabel}", html), ctx.CancellationToken);
    }
}

public sealed class DocumentRequestFullyApprovedConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<DocumentRequestFullyApprovedMessage>
{
    public async Task Consume(ConsumeContext<DocumentRequestFullyApprovedMessage> ctx)
    {
        var msg = ctx.Message;
        var model = new
        {
            msg.Title,
            msg.SubcontractorName,
            RequestsUrl = $"{links.Value.WebBaseUrl}/document-requests"
        };
        var html = await renderer.RenderAsync("document-request-fully-approved.cshtml", model);
        await sender.SendAsync(new EmailMessage(
            msg.InitiatorEmail, null,
            $"Заявка «{msg.Title}» полностью согласована", html), ctx.CancellationToken);
    }
}

public sealed class EmployeeQuizReminderConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<EmployeeQuizReminderMessage>
{
    public async Task Consume(ConsumeContext<EmployeeQuizReminderMessage> ctx)
    {
        var msg = ctx.Message;
        var model = new
        {
            msg.EmployeeFullName,
            msg.SubcontractorName,
            PortalUrl = links.Value.EmployeePortalBaseUrl ?? "http://localhost:5175"
        };
        var html = await renderer.RenderAsync("employee-quiz-reminder.cshtml", model);
        await sender.SendAsync(new EmailMessage(
            msg.Email, msg.EmployeeFullName,
            "Пройдите опрос по безопасности перед выходом на объект", html), ctx.CancellationToken);
    }
}

public sealed class ApprovalSlaWarningConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer)
    : IConsumer<ApprovalSlaWarningMessage>
{
    public async Task Consume(ConsumeContext<ApprovalSlaWarningMessage> ctx)
    {
        var msg = ctx.Message;
        var html = await renderer.RenderAsync("approval-sla-warning.cshtml", msg);
        await sender.SendAsync(new EmailMessage(
            msg.ApproverEmail, msg.ApproverFullName,
            $"Просрочено согласование: {msg.SubjectTitle} ({msg.PendingDays} дн.)", html), ctx.CancellationToken);
    }
}

public sealed class ApprovalEscalationConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer)
    : IConsumer<ApprovalEscalationMessage>
{
    public async Task Consume(ConsumeContext<ApprovalEscalationMessage> ctx)
    {
        var msg = ctx.Message;
        var html = await renderer.RenderAsync("approval-escalation.cshtml", msg);
        await sender.SendAsync(new EmailMessage(
            msg.EscalationEmail, msg.EscalationFullName,
            $"Эскалация: {msg.SubjectTitle} ({msg.PendingDays} дн.)", html), ctx.CancellationToken);
    }
}
