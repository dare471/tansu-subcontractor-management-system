using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public AuthTokenResult IssueLocalToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured or shorter than 32 characters.");

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.Email),
            new("user_type", user.UserType),
            new("must_change_password", user.MustChangePassword ? "true" : "false"),
            new("is_superuser", user.IsSuperUser ? "true" : "false"),
        };

        if (user.SubcontractorId is { } sid)
            claims.Add(new Claim("subcontractor_id", sid.ToString()));

        if (user.EmployeeId is { } eid)
            claims.Add(new Claim("employee_id", eid.ToString()));

        if (!string.IsNullOrWhiteSpace(user.ApproverRole))
            claims.Add(new Claim("approver_role", user.ApproverRole));

        if (!string.IsNullOrWhiteSpace(user.TansuRole))
            claims.Add(new Claim("tansu_role", user.TansuRole));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new AuthTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
