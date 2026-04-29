using Mealie.Application.Dtos.Groups;

namespace Mealie.Application.Services.Households;

public interface IHouseholdService
{
    Task<HouseholdResponse?> GetHouseholdAsync(Guid householdId, CancellationToken ct = default);
    Task<IList<HouseholdResponse>> GetHouseholdsForGroupAsync(Guid groupId, CancellationToken ct = default);
    Task<HouseholdResponse?> UpdateHouseholdAsync(Guid householdId, UpdateHouseholdRequest request, CancellationToken ct = default);
    Task<HouseholdResponse> CreateHouseholdAsync(Guid groupId, CreateHouseholdRequest request, CancellationToken ct = default);
    Task<bool> DeleteHouseholdAsync(Guid householdId, CancellationToken ct = default);
    Task<IList<UserSummaryDto>> GetMembersAsync(Guid householdId, CancellationToken ct = default);
    Task<HouseholdStatisticsResponse> GetStatisticsAsync(Guid householdId, CancellationToken ct = default);
    Task<HouseholdPreferencesResponse?> GetHouseholdPreferencesAsync(Guid householdId, CancellationToken ct = default);
    Task<HouseholdPreferencesResponse?> UpdateHouseholdPreferencesAsync(Guid householdId, UpdateHouseholdPreferencesRequest request, CancellationToken ct = default);
    Task<InviteTokenResponse> CreateHouseholdInviteTokenAsync(Guid groupId, Guid householdId, CreateInviteTokenRequest request, CancellationToken ct = default);
    Task<bool> UpdateMemberPermissionsAsync(Guid householdId, Guid userId, bool admin, bool canOrganize, bool canInvite, CancellationToken ct = default);
}
