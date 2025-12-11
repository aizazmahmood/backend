using System.Security.Claims;
using backend.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace backend.Auth;

public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int UserId =>
        int.TryParse(Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : 0;

    public string OrgId => Principal?.FindFirst("orgId")?.Value ?? string.Empty;

    public IReadOnlyCollection<Role> Roles
    {
        get
        {
            var roleNames = Principal?
                .FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat(Principal?.FindAll("roles").Select(c => c.Value) ?? Array.Empty<string>())
                .Distinct() ?? Array.Empty<string>();

            var roles = new List<Role>();
            foreach (var rn in roleNames)
            {
                if (Enum.TryParse<Role>(rn, out var r))
                {
                    roles.Add(r);
                }
            }
            return roles;
        }
    }

    public bool IsAdmin => Roles.Contains(Role.Admin);
    public bool IsModerator => Roles.Contains(Role.Moderator);
}
