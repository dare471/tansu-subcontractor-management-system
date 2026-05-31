using System.Reflection;

namespace Tansu.Infrastructure.Seeding;

internal static class DemoPortraitAsset
{
    private static readonly Lazy<byte[]> BytesLazy = new(Load);

    public static byte[] Bytes => BytesLazy.Value;

    private static byte[] Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Tansu.Infrastructure.Seeding.Assets.demo-portrait.jpg";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource not found: {resourceName}");

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
