using backend.Models;

namespace backend.Auth;

public class RbacService
{
    public bool CanManageEvent(int userId, string orgId, IReadOnlyCollection<Role> roles, Event ev)
    {
        if (roles.Contains(Role.Admin))
            return true;

        if (roles.Contains(Role.Moderator))
            return ev.OrgId == orgId;

        // basic user
        return ev.OrgId == orgId && ev.CreatorId == userId;
    }
}
