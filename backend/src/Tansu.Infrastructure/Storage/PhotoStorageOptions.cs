namespace Tansu.Infrastructure.Storage;

public class PhotoStorageOptions
{
    public const string SectionName = "App";
    public string PhotoStoragePath { get; set; } = "/var/lib/tansu/photos";
}
