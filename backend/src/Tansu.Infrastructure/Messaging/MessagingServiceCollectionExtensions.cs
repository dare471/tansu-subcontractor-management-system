using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tansu.Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddTansuMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var host = configuration["RabbitMq:Host"];
        var user = configuration["RabbitMq:User"] ?? "guest";
        var pass = configuration["RabbitMq:Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            if (string.IsNullOrWhiteSpace(host))
            {
                x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
            }
            else
            {
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(host, "/", h =>
                    {
                        h.Username(user);
                        h.Password(pass);
                    });
                    cfg.ConfigureEndpoints(ctx);
                });
            }
        });

        return services;
    }
}
