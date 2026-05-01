using Mealie.Application.Dtos.Admin;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Admin;

public record GetAdminUserQuery(Guid UserId) : IQuery<AdminUserResponse?>
{
    public async Task<AdminUserResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var u = await services.Db.Users.IgnoreQueryFilters()
            .Include(u => u.Group).Include(u => u.Household)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        return u is null ? null : AdminUserMappings.MapToResponse(u);
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
