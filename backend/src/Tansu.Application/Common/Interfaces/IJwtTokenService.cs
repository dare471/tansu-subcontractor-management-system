using Tansu.Domain.Entities;

namespace Tansu.Application.Common.Interfaces;

public interface IJwtTokenService
{
    AuthTokenResult IssueLocalToken(User user);
}

public sealed record AuthTokenResult(string AccessToken, DateTimeOffset ExpiresAt);
