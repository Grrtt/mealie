using Mealie.Application.Dtos.Users;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Users;

public record GetUserProfileQuery(Guid UserId) : IQuery<UserResponse?>
{
    public async Task<UserResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var user = await services.Db.Users.IgnoreQueryFilters()
            .Include(u => u.Group).Include(u => u.Household)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        return user is null ? null : UserMappings.MapToResponse(user);
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
