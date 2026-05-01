using Mealie.Application.Dtos.Users;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

using Mealie.Application.Queries;
namespace Mealie.Application.Commands.Users;

public record UpdateUserProfileCommand(Guid UserId, UpdateUserRequest Request) : IQuery<UserResponse?>
{
    public async Task<UserResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.Group).Include(u => u.Household)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return null;
        if (Request.FullName is not null) user.FullName = Request.FullName;
        if (Request.Email is not null) user.Email = Request.Email;
        if (Request.Username is not null) user.Username = Request.Username;
        user.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return UserMappings.MapToResponse(user);
    }
}

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null || user.Password is null) return false;
        if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, user.Password)) return false;
        user.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
        user.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

file static class UserMappings
{
    public static UserResponse MapToResponse(User u) =>
        new()
        {
            Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email,
            AuthMethod = u.AuthMethod.ToString(), Admin = u.Admin, Advanced = u.Advanced,
            GroupId = u.GroupId, Group = u.Group?.Name ?? string.Empty,
            GroupSlug = u.Group?.Slug ?? string.Empty,
            HouseholdId = u.HouseholdId, Household = u.Household?.Name ?? string.Empty,
            HouseholdSlug = u.Household?.Slug ?? string.Empty,
            CanManageHousehold = u.CanManageHousehold, CanManage = u.CanManage,
            CanInvite = u.CanInvite, CanOrganize = u.CanOrganize
        };
}
