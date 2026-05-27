using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Auth;

namespace Tansu.Api.Auth;

public static class JwtSetup
{
    public static IServiceCollection AddTansuAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var entra = configuration.GetSection(EntraOptions.SectionName).Get<EntraOptions>() ?? new EntraOptions();

        services.AddHttpContextAccessor();

        var authBuilder = services
            .AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = AuthSchemes.LocalJwt;
                o.DefaultChallengeScheme = AuthSchemes.LocalJwt;
            })
            .AddJwtBearer(AuthSchemes.LocalJwt, opts =>
            {
                opts.RequireHttpsMetadata = false;
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = string.IsNullOrWhiteSpace(jwt.SigningKey)
                        ? null
                        : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        if (entra.IsConfigured)
        {
            authBuilder.AddJwtBearer(AuthSchemes.Entra, opts =>
            {
                opts.Authority = entra.Authority;
                opts.Audience = entra.Audience;
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                opts.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var provisioner = context.HttpContext.RequestServices
                            .GetRequiredService<EntraUserProvisioner>();
                        await provisioner.ProvisionAsync(context);
                    }
                };
            });
        }

        services.AddAuthorization(o =>
        {
            o.AddPolicy(AuthPolicies.TansuOnly, p =>
            {
                p.RequireAuthenticatedUser();
                p.AddAuthenticationSchemes(AuthSchemes.LocalJwt, AuthSchemes.Entra);
                p.RequireAssertion(ctx =>
                    string.Equals(ctx.User.FindFirst("user_type")?.Value, UserType.Tansu, StringComparison.Ordinal)
                    || ctx.User.Identity?.AuthenticationType == AuthSchemes.Entra);
            });

            o.AddPolicy(AuthPolicies.SubcontractorOnly, p =>
            {
                p.RequireAuthenticatedUser();
                p.AddAuthenticationSchemes(AuthSchemes.LocalJwt);
                p.RequireClaim("user_type", UserType.Subcontractor);
            });

            o.AddPolicy(AuthPolicies.EmployeeOnly, p =>
            {
                p.RequireAuthenticatedUser();
                p.AddAuthenticationSchemes(AuthSchemes.LocalJwt);
                p.RequireClaim("user_type", UserType.Employee);
            });
        });

        services.AddScoped<EntraUserProvisioner>();

        return services;
    }
}
