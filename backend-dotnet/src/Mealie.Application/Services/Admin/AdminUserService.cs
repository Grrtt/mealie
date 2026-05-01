using Mealie.Application.Dtos.Admin;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Admin;

public class AdminUserService(ApplicationDbContext db) : IAdminUserService
{
    public async Task<object> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await db.Users.IgnoreQueryFilters()
            .Include(u => u.Group)
            .Include(u => u.Household)
            .ToListAsync(ct);
        var items = users.Select(MapToResponse).ToList();
        return new
        {
            page = 1,
            per_page = -1,
            total = items.Count,
            total_pages = 1,
            items
        };
    }

    public async Task<AdminUserResponse?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var u = await db.Users.IgnoreQueryFilters()
            .Include(u => u.Group)
            .Include(u => u.Household)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        return u is null ? null : MapToResponse(u);
    }

    public async Task<AdminUserResponse?> CreateUserAsync(CreateAdminUserRequest request,
        CancellationToken ct = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            AuthMethod = AuthMethod.Mealie,
            Admin = request.Admin,
            GroupId = request.GroupId,
            HouseholdId = request.HouseholdId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return MapToResponse(user);
    }

    public async Task<AdminUserResponse?> UpdateUserAsync(Guid userId, UpdateAdminUserRequest request,
        CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.Group)
            .Include(u => u.Household)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return null;
        }

        if (request.FullName is not null)
        {
            user.FullName = request.FullName;
        }

        if (request.Email is not null)
        {
            user.Email = request.Email;
        }

        if (request.Password is not null)
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        if (request.Admin.HasValue)
        {
            user.Admin = request.Admin.Value;
        }

        if (request.Advanced.HasValue)
        {
            user.Advanced = request.Advanced.Value;
        }

        if (request.HouseholdId.HasValue)
        {
            user.HouseholdId = request.HouseholdId.Value;
        }
        else if (request.Household is not null)
        {
            var hh = await db.Households.IgnoreQueryFilters()
                .FirstOrDefaultAsync(h => h.Name == request.Household || h.Slug == request.Household, ct);
            if (hh is not null)
            {
                user.HouseholdId = hh.Id;
            }
        }

        if (request.CanManageHousehold.HasValue)
        {
            user.CanManageHousehold = request.CanManageHousehold.Value;
        }

        if (request.CanManage.HasValue)
        {
            user.CanManage = request.CanManage.Value;
        }

        if (request.CanInvite.HasValue)
        {
            user.CanInvite = request.CanInvite.Value;
        }

        if (request.CanOrganize.HasValue)
        {
            user.CanOrganize = request.CanOrganize.Value;
        }

        user.UpdateAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await db.Entry(user).Reference(u => u.Group).LoadAsync(ct);
        await db.Entry(user).Reference(u => u.Household).LoadAsync(ct);
        return MapToResponse(user);
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return false;
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UnlockUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return false;
        }

        user.LockedAt = null;
        user.LoginAttempts = 0;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static AdminUserResponse MapToResponse(User u)
    {
        return new AdminUserResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Username = u.Username,
            Email = u.Email,
            Admin = u.Admin,
            Advanced = u.Advanced,
            GroupId = u.GroupId,
            Group = u.Group?.Name,
            HouseholdId = u.HouseholdId,
            Household = u.Household?.Name,
            CanManageHousehold = u.CanManageHousehold,
            CanManage = u.CanManage,
            CanInvite = u.CanInvite,
            CanOrganize = u.CanOrganize,
            LoginAttempts = u.LoginAttempts,
            LockedAt = u.LockedAt,
            CreatedAt = u.CreatedAt,
            UpdateAt = u.UpdateAt
        };
    }
}
