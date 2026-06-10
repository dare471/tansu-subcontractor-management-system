public sealed class FaceVerifyOptions
{
    public const string SectionName = "FaceVerify";
    public string BaseUrl { get; set; } = "http://face-verify:8092";
    public int TimeoutSeconds { get; set; } = 120;
}

public interface IFaceVerificationService
{
    Task<FaceVerificationResult> VerifyAsync(
        byte[] referencePhoto,
        byte[] livePhoto,
        CancellationToken cancellationToken = default);
}

public sealed record FaceVerificationResult(bool Matched, double Confidence, string Message);

public sealed class StubFaceVerificationService : IFaceVerificationService
{
    public Task<FaceVerificationResult> VerifyAsync(
        byte[] referencePhoto,
        byte[] livePhoto,
        CancellationToken cancellationToken = default)
    {
        if (referencePhoto.Length < 100 || livePhoto.Length < 100)
        {
            return Task.FromResult(new FaceVerificationResult(false, 0,
                "Недостаточное качество изображения."));
        }

        var matched = referencePhoto.Length > 0 && livePhoto.Length > 0;
        return Task.FromResult(new FaceVerificationResult(
            matched,
            matched ? 0.92 : 0,
            matched
                ? "Лицо подтверждено."
                : "Лицо не подтверждено."));
    }
}

public sealed class RemoteFaceVerificationService(
    HttpClient http,
    Microsoft.Extensions.Options.IOptions<FaceVerifyOptions> options,
    ILogger<RemoteFaceVerificationService> logger) : IFaceVerificationService
{
    private readonly FaceVerifyOptions _options = options.Value;

    public async Task<FaceVerificationResult> VerifyAsync(
        byte[] referencePhoto,
        byte[] livePhoto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(referencePhoto), "referencePhoto", "reference.jpg");
            content.Add(new ByteArrayContent(livePhoto), "livePhoto", "live.jpg");

            using var response = await http.PostAsync("/api/verify", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("face-verify HTTP {Status}: {Body}", (int)response.StatusCode, body);
                return new FaceVerificationResult(false, 0,
                    "Сервис распознавания лиц недоступен. Повторите позже.");
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<FaceVerifyResponse>(
                body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result is null)
            {
                return new FaceVerificationResult(false, 0, "Некорректный ответ сервиса Face ID.");
            }

            return new FaceVerificationResult(result.Matched, result.Confidence, result.Message);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "face-verify connection failed");
            return new FaceVerificationResult(false, 0,
                "Сервис распознавания лиц недоступен.");
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "face-verify timeout");
            return new FaceVerificationResult(false, 0,
                "Превышено время ожидания проверки Face ID. Повторите попытку.");
        }
    }

    private sealed record FaceVerifyResponse(bool Matched, double Confidence, string Message);
}
