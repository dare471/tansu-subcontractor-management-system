using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.Configure<TansuApiOptions>(builder.Configuration.GetSection(TansuApiOptions.SectionName));
builder.Services.AddHttpClient<ITansuApiClient, TansuApiClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TansuApiOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
});

builder.Services.Configure<FaceVerifyOptions>(builder.Configuration.GetSection(FaceVerifyOptions.SectionName));
var faceVerifyUrl = builder.Configuration[$"{FaceVerifyOptions.SectionName}:BaseUrl"];
if (!string.IsNullOrWhiteSpace(faceVerifyUrl))
{
    builder.Services.AddHttpClient<IFaceVerificationService, RemoteFaceVerificationService>((sp, client) =>
    {
        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FaceVerifyOptions>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(Math.Max(30, opts.TimeoutSeconds));
    });
}
else
{
    builder.Services.AddSingleton<IFaceVerificationService, StubFaceVerificationService>();
}

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors();

app.MapHealthChecks("/health");

app.MapGet("/api/scan/{token}", async (string token, ITansuApiClient tansu, CancellationToken ct) =>
{
    var lookup = await tansu.GetPassAsync(token, ct);
    return lookup is null ? Results.NotFound() : Results.Ok(lookup);
});

app.MapPost("/api/verify/face", async (
    HttpRequest request,
    ITansuApiClient tansu,
    IFaceVerificationService faceVerification,
    CancellationToken ct) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { detail = "Ожидается multipart/form-data." });

    var form = await request.ReadFormAsync(ct);
    var token = form["token"].FirstOrDefault()?.Trim();
    var livePhoto = form.Files.GetFile("livePhoto") ?? form.Files.FirstOrDefault();

    if (string.IsNullOrEmpty(token) || livePhoto is null || livePhoto.Length == 0)
        return Results.BadRequest(new { detail = "Нужны token и livePhoto." });

    var lookup = await tansu.GetPassAsync(token, ct);
    if (lookup is null || !lookup.IsActive)
        return Results.NotFound(new { detail = "Пропуск не найден или отозван." });

    if (!lookup.HasReferencePhoto)
    {
        return Results.Ok(new
        {
            matched = false,
            confidence = 0,
            message = "У сотрудника нет эталонного фото для Face ID.",
            employee = lookup
        });
    }

    await using var liveStream = livePhoto.OpenReadStream();
    using var liveMs = new MemoryStream();
    await liveStream.CopyToAsync(liveMs, ct);
    var liveBytes = liveMs.ToArray();

    var referenceBytes = await tansu.GetReferencePhotoAsync(token, ct);
    if (referenceBytes is null || referenceBytes.Length == 0)
    {
        return Results.Ok(new
        {
            matched = false,
            confidence = 0,
            message = "Не удалось загрузить эталонное фото.",
            employee = lookup
        });
    }

    var result = await faceVerification.VerifyAsync(referenceBytes, liveBytes, ct);

    EmployeeSiteVisitRecord? visit = null;
    if (result.Matched)
    {
        visit = await tansu.RecordSiteVisitAsync(token, result.Confidence, ct);
    }

    return Results.Ok(new
    {
        matched = result.Matched,
        confidence = result.Confidence,
        message = result.Message,
        employee = lookup,
        siteVisitRecorded = visit is not null,
        siteVisit = visit
    });
})
.DisableAntiforgery();

app.Run();

public sealed class TansuApiOptions
{
    public const string SectionName = "TansuApi";
    public string BaseUrl { get; set; } = "http://api:8080";
    public string VerifyServiceKey { get; set; } = "dev-verify-service-key-change-me";
}

public interface ITansuApiClient
{
    Task<PassLookup?> GetPassAsync(string token, CancellationToken ct);
    Task<byte[]?> GetReferencePhotoAsync(string token, CancellationToken ct);
    Task<EmployeeSiteVisitRecord?> RecordSiteVisitAsync(string token, double faceConfidence, CancellationToken ct);
}

public sealed record EmployeeSiteVisitRecord(
    Guid Id,
    Guid EmployeeId,
    string EmployeeFullName,
    string? ProjectName,
    DateTimeOffset CheckedInAt,
    double? FaceConfidence,
    string VerificationMethod);

public sealed record PassLookup(
    Guid EmployeeId,
    string FullName,
    string Position,
    string SubcontractorName,
    string? ProjectName,
    bool HasReferencePhoto,
    DateTimeOffset IssuedAt,
    bool IsActive);

public sealed class TansuApiClient(HttpClient http, Microsoft.Extensions.Options.IOptions<TansuApiOptions> options)
    : ITansuApiClient
{
    private readonly TansuApiOptions _options = options.Value;

    public async Task<PassLookup?> GetPassAsync(string token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/access-passes/{Uri.EscapeDataString(token)}");
        request.Headers.Add("X-Tansu-Verify-Key", _options.VerifyServiceKey);
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PassLookup>(cancellationToken: ct);
    }

    public async Task<byte[]?> GetReferencePhotoAsync(string token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/access-passes/{Uri.EscapeDataString(token)}/reference-photo");
        request.Headers.Add("X-Tansu-Verify-Key", _options.VerifyServiceKey);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<EmployeeSiteVisitRecord?> RecordSiteVisitAsync(
        string token,
        double faceConfidence,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/internal/access-passes/{Uri.EscapeDataString(token)}/check-in");
        request.Headers.Add("X-Tansu-Verify-Key", _options.VerifyServiceKey);
        request.Content = JsonContent.Create(new { faceConfidence });
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmployeeSiteVisitRecord>(cancellationToken: ct);
    }
}

public partial class Program;
