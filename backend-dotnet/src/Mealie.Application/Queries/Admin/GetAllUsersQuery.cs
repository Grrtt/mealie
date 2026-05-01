using Mealie.Application.Dtos.Admin;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Admin;

public record GetAllUsersQuery : IQuery<object>
{
    public async Task<object> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var users = await services.Db.Users.IgnoreQueryFilters()
            .Include(u => u.Group).Include(u => u.Household).ToListAsync(ct);
        var items = users.Select(AdminUserMappings.MapToResponse).ToList();
        return new { page = 1, per_page = -1, total = items.Count, total_pages = 1, items };
    }
}

file static class AdminUserMappings
{
    public static AdminUserResponse MapToResponse(User u)
    {
        return new AdminUserResponse
        {
            Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email,
            Admin = u.Admin, Advanced = u.Advanced, GroupId = u.GroupId, Group = u.Group?.Name,
            HouseholdId = u.HouseholdId, Household = u.Household?.Name,
            CanManageHousehold = u.CanManageHousehold, CanManage = u.CanManage,
            CanInvite = u.CanInvite, CanOrganize = u.CanOrganize,
            LoginAttempts = u.LoginAttempts, LockedAt = u.LockedAt,
            CreatedAt = u.CreatedAt, UpdateAt = u.UpdateAt
        };
    }
}
