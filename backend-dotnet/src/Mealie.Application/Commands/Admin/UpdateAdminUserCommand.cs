using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Admin;

public record UpdateAdminUserCommand(Guid UserId, UpdateAdminUserRequest Request) : IQuery<AdminUserResponse?>
{
    public async Task<AdminUserResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.Group).Include(u => u.Household)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return null;

        if (Request.FullName is not null) user.FullName = Request.FullName;
        if (Request.Email is not null) user.Email = Request.Email;
        if (Request.Password is not null) user.Password = BCrypt.Net.BCrypt.HashPassword(Request.Password);
        if (Request.Admin.HasValue) user.Admin = Request.Admin.Value;
        if (Request.Advanced.HasValue) user.Advanced = Request.Advanced.Value;
        if (Request.CanManageHousehold.HasValue) user.CanManageHousehold = Request.CanManageHousehold.Value;
        if (Request.CanManage.HasValue) user.CanManage = Request.CanManage.Value;
        if (Request.CanInvite.HasValue) user.CanInvite = Request.CanInvite.Value;
        if (Request.CanOrganize.HasValue) user.CanOrganize = Request.CanOrganize.Value;

        if (Request.HouseholdId.HasValue)
        {
            user.HouseholdId = Request.HouseholdId.Value;
        }
        else if (Request.Household is not null)
        {
            var hh = await db.Households.IgnoreQueryFilters()
                .FirstOrDefaultAsync(h => h.Name == Request.Household || h.Slug == Request.Household, ct);
            if (hh is not null) user.HouseholdId = hh.Id;
        }

        user.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await db.Entry(user).Reference(u => u.Group).LoadAsync(ct);
        await db.Entry(user).Reference(u => u.Household).LoadAsync(ct);
        return AdminUserMappings.MapToResponse(user);
    }
}

file static class AdminUserMappings
{
    public static Mealie.Application.Dtos.Admin.AdminUserResponse MapToResponse(Mealie.Domain.Entities.Core.User u) =>
        new()
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