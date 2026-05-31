using Tansu.Api.Auth;
using Tansu.Api.Endpoints;
using Tansu.Api.Middleware;
using Tansu.Application;
using Tansu.Application.Common.Interfaces;
using Tansu.Infrastructure;
using Tansu.Infrastructure.Messaging;
using Tansu.Infrastructure.Persistence;
using Tansu.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTansuInfrastructure(builder.Configuration);
builder.Services.AddTansuApplication();
builder.Services.AddTansuMessaging(builder.Configuration);
builder.Services.AddTansuAuth(builder.Configuration);

builder.Services.AddScoped<ICurrentUser, CurrentUserAccessor>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

builder.Services.AddHostedService<Tansu.Infrastructure.EmployeeDocuments.DocumentExpiryNotificationHostedService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
    if (app.Environment.IsDevelopment())
    {
        await DemoSeeder.SeedAsync(scope.ServiceProvider);
        await DemoSeeder.EnsureKazakhCompanyProfilesAsync(scope.ServiceProvider);
        await DemoSeeder.EnsureSubcontractorCredentialsAsync(scope.ServiceProvider);
        await DemoApproversSeeder.EnsureAsync(scope.ServiceProvider);
        await DemoDocumentRequestsSeeder.EnsureAsync(scope.ServiceProvider);
        await DemoSampleApprovalsSeeder.EnsureAsync(scope.ServiceProvider);
        await DemoEmployeePhotosSeeder.EnsureAsync(scope.ServiceProvider);
        await DemoAccessPassesSeeder.EnsureAsync(scope.ServiceProvider);
        await DemoEmployeePortalSeeder.EnsureAsync(scope.ServiceProvider);
        await DemoTansuRolesSeeder.EnsureAsync(scope.ServiceProvider);
        await DemoProjectDetailsSeeder.EnsureAsync(scope.ServiceProvider);
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<MustChangePasswordMiddleware>();

app.MapOpenApi();
app.MapGet("/swagger", () => Results.Redirect("/openapi/v1.json"));

app.MapHealthChecks("/health");
app.MapAuthEndpoints(app.Environment);
app.MapSubcontractorEndpoints();
app.MapUserEndpoints();
app.MapProjectEndpoints();
app.MapMatrixEndpoints();
app.MapEmployeeEndpoints();
app.MapEmployeePhotoReviewEndpoints();
app.MapSiteVisitJournalEndpoints();
app.MapAccessPassEndpoints();
app.MapInternalVerifyEndpoints();
app.MapEmployeePortalEndpoints();
app.MapApprovalEndpoints();
app.MapEmployeeBatchEndpoints();
app.MapDocumentRequestEndpoints();

app.Run();

public partial class Program;
