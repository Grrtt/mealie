using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;

namespace Mealie.Application.Commands.Admin;

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
