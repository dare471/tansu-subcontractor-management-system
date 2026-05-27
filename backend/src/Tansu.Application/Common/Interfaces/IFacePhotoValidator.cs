namespace Tansu.Application.Common.Interfaces;

public sealed record FacePhotoValidationResult(bool HasFace, string Message);

public interface IFacePhotoValidator
{
    Task<FacePhotoValidationResult> ValidateHasFaceAsync(Stream photo, CancellationToken ct);
}
