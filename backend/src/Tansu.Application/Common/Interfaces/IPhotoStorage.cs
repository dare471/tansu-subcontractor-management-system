namespace Tansu.Application.Common.Interfaces;

public interface IPhotoStorage
{
    Task<string> SaveAsync(Guid employeeId, string fileName, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
}
