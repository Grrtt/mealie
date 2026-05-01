using System.Security.Claims;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Data;

namespace Mealie.Api.Middleware;

public class TenantContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, TenantFilter tenantFilter)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? context.User.FindFirstValue("sub");
            var groupIdStr = context.User.FindFirstValue("group_id");
            var householdIdStr = context.User.FindFirstValue("household_id");
            var isAdmin = context.User.IsInRole("admin") || context.User.FindFirstValue("admin") == "true";

            if (Guid.TryParse(userIdStr, out var userId) &&
                Guid.TryParse(groupIdStr, out var groupId) &&
                Guid.TryParse(householdIdStr, out var householdId))
            {
                tenantContext.SetContext(groupId, householdId, userId, isAdmin);
                tenantFilter.GroupId = groupId;
                tenantFilter.HouseholdId = householdId;
            }
        }

        await next(context);
    }
}
