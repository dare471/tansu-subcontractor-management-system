namespace Tansu.Application.Common.Interfaces;

public interface IEmployeeDocumentStorage
{
    Task<string> SaveAsync(Guid employeeId, Guid documentId, string fileName, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
}
