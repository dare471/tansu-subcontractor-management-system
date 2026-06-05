using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;
using Tansu.Application.Auth;
using Tansu.Application.Common;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Common.Behaviors;

namespace Tansu.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddTansuApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        services.Configure<BrandingOptions>(configuration.GetSection(BrandingOptions.SectionName));
        services.AddSingleton<IAppBranding, AppBranding>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddScoped<ITansuAccessService, TansuAccessService>();

        return services;
    }
}
