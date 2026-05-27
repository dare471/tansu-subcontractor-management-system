using System.Security.Cryptography;

namespace Tansu.Infrastructure.AccessPasses;

public sealed class AccessPassTokenGenerator : Application.Common.Interfaces.IAccessPassTokenGenerator
{
    public string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
}
