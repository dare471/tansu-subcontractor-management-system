using Microsoft.Extensions.Options;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Infrastructure.Storage;

public sealed class LocalEmployeeDocumentStorage : IEmployeeDocumentStorage
{
    private readonly string _baseRoot;

    public LocalEmployeeDocumentStorage(IOptions<PhotoStorageOptions> options)
    {
        _baseRoot = options.Value.PhotoStoragePath;
        Directory.CreateDirectory(Path.Combine(_baseRoot, "documents"));
    }

    public async Task<string> SaveAsync(
        Guid employeeId,
        Guid documentId,
        string fileName,
        Stream content,
        CancellationToken ct)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".bin";

        var dir = Path.Combine(_baseRoot, "documents", employeeId.ToString());
        Directory.CreateDirectory(dir);

        var fileNameOnDisk = $"{documentId:N}{ext.ToLowerInvariant()}";
        var full = Path.Combine(dir, fileNameOnDisk);
        var relative = Path.Combine("documents", employeeId.ToString(), fileNameOnDisk);

        await using (var fs = File.Create(full))
            await content.CopyToAsync(fs, ct);

        return relative.Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        var full = Path.Combine(_baseRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(full));
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct)
    {
        var full = Path.Combine(_baseRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }
}
