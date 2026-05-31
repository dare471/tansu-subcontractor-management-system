using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tansu.Application.Common.Interfaces;
using Tansu.Infrastructure.FaceVerify;

namespace Tansu.Infrastructure.FaceVerify;

public sealed class RemoteReferencePhotoValidator(
    HttpClient http,
    IOptions<FaceVerifyOptions> options,
    ILogger<RemoteReferencePhotoValidator> logger) : IReferencePhotoValidator
{
    private readonly FaceVerifyOptions _options = options.Value;

    public async Task<ReferencePhotoValidationResult> ValidateAsync(Stream photo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return StubReferencePhotoValidator.PassThrough();

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(photo);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "photo", "photo.jpg");

        try
        {
            var response = await http.PostAsync("/api/validate-reference-photo", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("validate-reference-photo HTTP {Status}: {Body}", (int)response.StatusCode, body);
                return Failed("Сервис проверки фото недоступен.");
            }

            var result = await response.Content.ReadFromJsonAsync<ValidateResponse>(ct);
            if (result is null)
                return Failed("Некорректный ответ сервиса проверки фото.");

            var checks = result.Checks?.Select(c => new PhotoValidationCheck(
                c.Code ?? "unknown",
                c.Passed,
                c.Message ?? "")).ToList() ?? [];

            return new ReferencePhotoValidationResult(
                result.Valid,
                result.Width,
                result.Height,
                result.FileSize,
                result.FaceCount,
                result.Message ?? "",
                checks);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "validate-reference-photo failed");
            return Failed("Сервис проверки фото недоступен.");
        }
    }

    private static ReferencePhotoValidationResult Failed(string message) =>
        new(false, 0, 0, 0, 0, message, []);

    private sealed record ValidateResponse(
        bool Valid,
        int Width,
        int Height,
        int FileSize,
        int FaceCount,
        string? Message,
        List<CheckResponse>? Checks);

    private sealed record CheckResponse(
        string? Code,
        bool Passed,
        string? Message);
}

public sealed class StubReferencePhotoValidator : IReferencePhotoValidator
{
    public Task<ReferencePhotoValidationResult> ValidateAsync(Stream photo, CancellationToken ct) =>
        Task.FromResult(PassThrough());

    internal static ReferencePhotoValidationResult PassThrough() =>
        new(
            true,
            640,
            480,
            60_000,
            1,
            "Автопроверка пропущена (FaceVerify не настроен).",
            [new PhotoValidationCheck("stub", true, "FaceVerify не настроен.")]);
}
