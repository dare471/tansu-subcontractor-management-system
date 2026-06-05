using MassTransit;
using Tansu.Worker.Consumers;
using Tansu.Worker.Email;
using Tansu.Worker.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<AppLinksOptions>(builder.Configuration.GetSection(AppLinksOptions.SectionName));
builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));

builder.Services.AddSingleton<IEmailTemplateRenderer, RazorLightEmailRenderer>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var mqHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
var mqUser = builder.Configuration["RabbitMq:User"] ?? "guest";
var mqPass = builder.Configuration["RabbitMq:Password"] ?? "guest";

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<PasswordResetConsumer>();
    x.AddConsumer<ApprovalSubmittedConsumer>();
    x.AddConsumer<NextApproverConsumer>();
    x.AddConsumer<ApprovalDecisionConsumer>();
    x.AddConsumer<EmployeeFullyApprovedConsumer>();
    x.AddConsumer<DocumentRequestSubmittedConsumer>();
    x.AddConsumer<DocumentRequestNextApproverConsumer>();
    x.AddConsumer<EmployeeBatchSubmittedConsumer>();
    x.AddConsumer<EmployeeBlockedConsumer>();
    x.AddConsumer<EmployeeDocumentExpiringConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(mqHost, "/", h =>
        {
            h.Username(mqUser);
            h.Password(mqPass);
        });

        cfg.UseMessageRetry(r => r.Exponential(
            5, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5)));

        cfg.ConfigureEndpoints(ctx);
    });
});

var host = builder.Build();
await host.RunAsync();
