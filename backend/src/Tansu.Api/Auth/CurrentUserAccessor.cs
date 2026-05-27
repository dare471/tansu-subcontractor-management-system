using System.Security.Claims;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Api.Auth;

public sealed class CurrentUserAccessor(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue("sub")
                      ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue("email")
        ?? Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("preferred_username");

    public string? UserType
    {
        get
        {
            var claim = Principal?.FindFirstValue("user_type");
            if (!string.IsNullOrEmpty(claim)) return claim;

            return Principal?.Identity?.AuthenticationType == AuthSchemes.Entra
                ? Domain.Enums.UserType.Tansu
                : null;
        }
    }

    public Guid? SubcontractorId
    {
        get
        {
            var raw = Principal?.FindFirstValue("subcontractor_id");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public Guid? EmployeeId
    {
        get
        {
            var raw = Principal?.FindFirstValue("employee_id");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool MustChangePassword =>
        string.Equals(Principal?.FindFirstValue("must_change_password"), "true", StringComparison.OrdinalIgnoreCase);
}
