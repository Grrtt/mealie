using Mealie.Application.Dtos.Groups;

namespace Mealie.Application.Services.Groups;

public interface IGroupService
{
    Task<GroupResponse?> GetGroupAsync(Guid groupId, CancellationToken ct = default);
    Task<GroupResponse?> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request, CancellationToken ct = default);
    Task<IList<UserSummaryDto>> GetMembersAsync(Guid groupId, CancellationToken ct = default);
    Task<UserSummaryDto?> GetMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default);
    Task<IList<HouseholdResponse>> GetHouseholdsAsync(Guid groupId, CancellationToken ct = default);
    Task<InviteTokenResponse> CreateInviteTokenAsync(Guid groupId, Guid? householdId, CancellationToken ct = default);
    Task<IList<InviteTokenResponse>> GetInviteTokensAsync(Guid groupId, CancellationToken ct = default);
    Task<bool> DeleteInviteTokenAsync(Guid groupId, Guid tokenId, CancellationToken ct = default);
    Task<GroupPreferencesResponse?> GetGroupPreferencesAsync(Guid groupId, CancellationToken ct = default);
    Task<GroupPreferencesResponse?> UpdateGroupPreferencesAsync(Guid groupId, UpdateGroupPreferencesRequest request, CancellationToken ct = default);
}
