using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.AccessPasses;
using Tansu.Application.EmployeePhotoReviews;
using Tansu.Application.EmployeePortal;
using Tansu.Infrastructure.AccessPasses;
using Tansu.Infrastructure.Auth;
using Tansu.Infrastructure.EmployeePortal;
using Tansu.Infrastructure.FaceVerify;
using Tansu.Infrastructure.Hik;
using Tansu.Infrastructure.Persistence;
using Tansu.Infrastructure.Storage;
using Tansu.Infrastructure.Zup;

namespace Tansu.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddTansuInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<TansuDbContext>(opts =>
        {
            opts.UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable("__ef_migrations_history", TansuDbContext.Schema));
        });

        services.AddScoped<ITansuDbContext>(sp => sp.GetRequiredService<TansuDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EntraOptions>(configuration.GetSection(EntraOptions.SectionName));
        services.Configure<PhotoStorageOptions>(configuration.GetSection(PhotoStorageOptions.SectionName));
        services.Configure<AccessPassOptions>(configuration.GetSection(AccessPassOptions.SectionName));
        services.Configure<EmployeePortalOptions>(configuration.GetSection(EmployeePortalOptions.SectionName));
        services.Configure<EmployeePhotoReviewOptions>(configuration.GetSection(EmployeePhotoReviewOptions.SectionName));
        services.Configure<FaceVerifyOptions>(configuration.GetSection(FaceVerifyOptions.SectionName));
        services.Configure<ZupOptions>(configuration.GetSection(ZupOptions.SectionName));

        var faceVerifyUrl = configuration[$"{FaceVerifyOptions.SectionName}:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(faceVerifyUrl))
        {
            services.AddHttpClient<IFacePhotoValidator, RemoteFacePhotoValidator>((sp, client) =>
            {
                var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FaceVerifyOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            services.AddHttpClient<IReferencePhotoValidator, RemoteReferencePhotoValidator>((sp, client) =>
            {
                var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FaceVerifyOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
        }
        else
        {
            services.AddSingleton<IFacePhotoValidator, StubFacePhotoValidator>();
            services.AddSingleton<IReferencePhotoValidator, StubReferencePhotoValidator>();
        }

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPhotoStorage, LocalPhotoStorage>();
        services.AddSingleton<IEmployeeDocumentStorage, LocalEmployeeDocumentStorage>();
        services.AddSingleton<IProjectDocumentStorage, LocalProjectDocumentStorage>();
        services.AddSingleton<ISubcontractorDocumentStorage, LocalSubcontractorDocumentStorage>();
        services.AddHttpClient<IZupAccessTokenProvider, ZupClientCredentialsTokenProvider>(client =>
            client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient<IZupEmployeeDirectory, HttpZupEmployeeDirectory>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ZupOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IHikAccessService, StubHikAccessService>();
        services.AddSingleton<IAccessPassQrEncoder, AccessPassQrEncoder>();
        services.AddSingleton<IAccessPassTokenGenerator, AccessPassTokenGenerator>();
        services.AddSingleton<IEmployeePortalCredentialWriter, FileEmployeePortalCredentialWriter>();

        return services;
    }
}
