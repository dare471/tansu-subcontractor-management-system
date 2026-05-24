using Microsoft.Extensions.Options;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Infrastructure.Storage;

public class LocalPhotoStorage : IPhotoStorage
{
    private readonly string _root;

    public LocalPhotoStorage(IOptions<PhotoStorageOptions> options)
    {
        _root = options.Value.PhotoStoragePath;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Guid employeeId, string fileName, Stream content, CancellationToken ct)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".bin";
        var dir = Path.Combine(_root, employeeId.ToString());
        Directory.CreateDirectory(dir);

        var relative = Path.Combine(employeeId.ToString(), $"photo{ext.ToLowerInvariant()}");
        var full = Path.Combine(_root, relative);

        await using (var fs = File.Create(full))
            await content.CopyToAsync(fs, ct);

        return relative.Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        var full = Path.Combine(_root, relativePath);
        if (!File.Exists(full)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(full));
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct)
    {
        var full = Path.Combine(_root, relativePath);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }
}
