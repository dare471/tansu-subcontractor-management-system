namespace Tansu.Application.Common.Interfaces;

public interface ISubcontractorDocumentStorage
{
    Task<string> SaveAsync(
        Guid subcontractorId,
        Guid documentId,
        string fileName,
        Stream content,
        CancellationToken ct);

    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
}
