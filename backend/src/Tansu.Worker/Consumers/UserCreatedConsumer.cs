using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tansu.Contracts.Messages;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

namespace Tansu.Worker.Consumers;

public sealed class UserCreatedConsumer(
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IOptions<AppLinksOptions> links,
    IOptions<BrandingOptions> branding,
    ILogger<UserCreatedConsumer> logger)
    : IConsumer<UserCreatedMessage>
{
    public async Task Consume(ConsumeContext<UserCreatedMessage> ctx)
    {
        var msg = ctx.Message;
        if (string.IsNullOrWhiteSpace(msg.TemporaryPassword))
        {
            logger.LogInformation("UserCreated for {Email}: no temp password (TANSU user); skipping email.", msg.Email);
            return;
        }

        var model = new
        {
            msg.FullName,
            msg.Email,
            msg.TemporaryPassword,
            LoginUrl = $"{links.Value.WebBaseUrl}/login"
        };

        var html = await renderer.RenderAsync("user-created.cshtml", model);
        await sender.SendAsync(
            new EmailMessage(
                msg.Email,
                msg.FullName,
                $"Доступ в систему {branding.Value.CompanyName}",
                html),
            ctx.CancellationToken);
    }
}
