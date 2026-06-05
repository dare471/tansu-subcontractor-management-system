namespace Tansu.Infrastructure.Zup;

public interface IZupAccessTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken ct);
}
