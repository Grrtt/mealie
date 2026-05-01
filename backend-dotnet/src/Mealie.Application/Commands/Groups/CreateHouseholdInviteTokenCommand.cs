using Mealie.Application.Common;
using Mealie.Application.Dtos.Groups;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Groups;

public record CreateHouseholdInviteTokenCommand(Guid GroupId, Guid HouseholdId, CreateInviteTokenRequest Request)
    : IQuery<InviteTokenResponse>
{
    public async Task<InviteTokenResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var token = new GroupInviteToken
        {
            Id = Guid.NewGuid(), Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            GroupId = GroupId, HouseholdId = HouseholdId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.InviteTokens.Add(token);
        await db.SaveChangesAsync(ct);
        return new InviteTokenResponse { Id = token.Id, Token = token.Token, GroupId = GroupId, HouseholdId = HouseholdId };
    }
}