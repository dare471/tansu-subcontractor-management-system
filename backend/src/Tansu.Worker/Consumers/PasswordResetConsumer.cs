using MassTransit;
using Microsoft.Extensions.Options;
using Tansu.Contracts.Messages;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

namespace Tansu.Worker.Consumers;

public sealed class PasswordResetConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links)
    : IConsumer<PasswordResetMessage>
{
    public async Task Consume(ConsumeContext<PasswordResetMessage> ctx)
    {
        var msg = ctx.Message;
        var model = new
        {
            msg.FullName,
            msg.Email,
            msg.TemporaryPassword,
            LoginUrl = $"{links.Value.WebBaseUrl}/login"
        };

        var html = await renderer.RenderAsync("password-reset.cshtml", model);
        await sender.SendAsync(
            new EmailMessage(msg.Email, msg.FullName, "Сброс пароля Tansu", html), ctx.CancellationToken);
    }
}
