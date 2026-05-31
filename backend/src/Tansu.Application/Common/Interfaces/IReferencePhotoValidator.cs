namespace Tansu.Application.Common.Interfaces;

public sealed record PhotoValidationCheck(string Code, bool Passed, string Message);

public sealed record ReferencePhotoValidationResult(
    bool Valid,
    int Width,
    int Height,
    int FileSize,
    int FaceCount,
    string Message,
    IReadOnlyList<PhotoValidationCheck> Checks);

public interface IReferencePhotoValidator
{
    Task<ReferencePhotoValidationResult> ValidateAsync(Stream photo, CancellationToken ct);
}
