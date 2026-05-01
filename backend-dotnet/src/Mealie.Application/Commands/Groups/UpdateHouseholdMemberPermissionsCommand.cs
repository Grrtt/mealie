using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Groups;

public record UpdateHouseholdMemberPermissionsCommand(
    Guid HouseholdId,
    Guid UserId,
    bool Admin,
    bool CanOrganize,
    bool CanInvite)
    : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == UserId && u.HouseholdId == HouseholdId, ct);
        if (user is null)
        {
            return false;
        }

        user.Admin = Admin;
        user.CanOrganize = CanOrganize;
        user.CanInvite = CanInvite;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
