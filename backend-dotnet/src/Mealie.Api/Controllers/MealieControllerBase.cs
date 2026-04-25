using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers;

[Authorize]
[ApiController]
public abstract class MealieControllerBase(ITenantContext tenantContext) : ControllerBase
{
    protected Guid CurrentGroupId => tenantContext.GroupId;
    protected Guid CurrentHouseholdId => tenantContext.HouseholdId;
    protected Guid CurrentUserId => tenantContext.UserId;
    protected bool CurrentUserIsAdmin => tenantContext.IsAdmin;

    /// <summary>
    /// Returns 404 regardless of whether the resource exists or belongs to a different household.
    /// This prevents data leakage about other tenants' resources.
    /// </summary>
    protected ActionResult NotFoundOrForbidden() => NotFound(new { detail = "Not found" });
}
