using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Tansu.Infrastructure.Hik;


internal static class HikCentralSigner
{
    private const string Accept = "application/json";
    private const string ContentType = "application/json";

    public static HttpRequestMessage CreateSignedJsonPost(
        string path,
        string jsonBody,
        string appKey,
        string appSecret)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
        var contentMd5 = Convert.ToBase64String(MD5.HashData(bodyBytes));

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            .ToString(CultureInfo.InvariantCulture);
        var nonce = Guid.NewGuid().ToString();

        var signedHeaders = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["x-ca-key"] = appKey,
            ["x-ca-nonce"] = nonce,
            ["x-ca-timestamp"] = timestamp,
        };

        var sb = new StringBuilder();
        sb.Append("POST").Append('\n');
        sb.Append(Accept).Append('\n');
        sb.Append(contentMd5).Append('\n');
        sb.Append(ContentType).Append('\n');
        sb.Append('\n'); // Date — пусто, используем x-ca-timestamp
        foreach (var (key, value) in signedHeaders)
            sb.Append(key).Append(':').Append(value).Append('\n');
        sb.Append(path);

        var signature = ComputeHmacSha256(appSecret, sb.ToString());

        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new ByteArrayContent(bodyBytes),
        };
        request.Content.Headers.Remove("Content-Type");
        request.Content.Headers.TryAddWithoutValidation("Content-Type", ContentType);

        request.Headers.TryAddWithoutValidation("Accept", Accept);
        request.Headers.TryAddWithoutValidation("Content-MD5", contentMd5);
        request.Headers.TryAddWithoutValidation("X-Ca-Key", appKey);
        request.Headers.TryAddWithoutValidation("X-Ca-Nonce", nonce);
        request.Headers.TryAddWithoutValidation("X-Ca-Timestamp", timestamp);
        request.Headers.TryAddWithoutValidation(
            "X-Ca-Signature-Headers", string.Join(',', signedHeaders.Keys));
        request.Headers.TryAddWithoutValidation("X-Ca-Signature", signature);

        return request;
    }

    private static string ComputeHmacSha256(string secret, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
