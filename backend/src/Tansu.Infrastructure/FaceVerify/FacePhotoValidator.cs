using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Infrastructure.FaceVerify;

public sealed class FaceVerifyOptions
{
    public const string SectionName = "FaceVerify";
    public string BaseUrl { get; set; } = "http://face-verify:8092";
}

public sealed class RemoteFacePhotoValidator(
    HttpClient http,
    IOptions<FaceVerifyOptions> options,
    ILogger<RemoteFacePhotoValidator> logger) : IFacePhotoValidator
{
    private readonly FaceVerifyOptions _options = options.Value;

    public async Task<FacePhotoValidationResult> ValidateHasFaceAsync(Stream photo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return new FacePhotoValidationResult(true, "Проверка лица пропущена (FaceVerify не настроен).");

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(photo);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "photo", "photo.jpg");

        try
        {
            var response = await http.PostAsync("/api/detect-face", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("face-verify detect HTTP {Status}: {Body}", (int)response.StatusCode, body);
                return new FacePhotoValidationResult(false, "Сервис проверки фото недоступен.");
            }

            var result = await response.Content.ReadFromJsonAsync<DetectFaceResponse>(ct);
            if (result is null)
                return new FacePhotoValidationResult(false, "Некорректный ответ сервиса Face ID.");

            return new FacePhotoValidationResult(result.HasFace, result.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "face-verify detect failed");
            return new FacePhotoValidationResult(false, "Сервис распознавания лиц недоступен.");
        }
    }

    private sealed record DetectFaceResponse(bool HasFace, string Message);
}

public sealed class StubFacePhotoValidator : IFacePhotoValidator
{
    public Task<FacePhotoValidationResult> ValidateHasFaceAsync(Stream photo, CancellationToken ct) =>
        Task.FromResult(new FacePhotoValidationResult(
            true,
            "Проверка лица пропущена (заглушка). Запустите face-verify для реальной проверки."));
}
