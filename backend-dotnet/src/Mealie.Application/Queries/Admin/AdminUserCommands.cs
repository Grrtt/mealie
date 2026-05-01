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

public record CreateAdminUserCommand(CreateAdminUserRequest Request) : IQuery<AdminUserResponse>
{
    public async Task<AdminUserResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = new User
        {
            Id = Guid.NewGuid(), Username = Request.Username, Email = Request.Email,
            FullName = Request.FullName, Password = BCrypt.Net.BCrypt.HashPassword(Request.Password),
            AuthMethod = AuthMethod.Mealie, Admin = Request.Admin,
            GroupId = Request.GroupId, HouseholdId = Request.HouseholdId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return AdminUserMappings.MapToResponse(user);
    }
}

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

public record DeleteAdminUserCommand(Guid UserId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record UnlockUserCommand(Guid UserId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return false;
        user.LockedAt = null;
        user.LoginAttempts = 0;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

file static class AdminUserMappings
{
    public static AdminUserResponse MapToResponse(User u) =>
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
