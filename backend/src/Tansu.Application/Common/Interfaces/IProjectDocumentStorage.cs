namespace Tansu.Application.Common.Interfaces;

public interface IProjectDocumentStorage
{
    Task<string> SaveAsync(Guid projectOid, Guid documentId, string fileName, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
}
