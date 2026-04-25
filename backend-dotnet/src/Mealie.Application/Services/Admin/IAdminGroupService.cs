using Mealie.Application.Dtos.Admin;

namespace Mealie.Application.Services.Admin;

public interface IAdminGroupService
{
    Task<object> GetAllGroupsAsync(CancellationToken ct = default);
    Task<AdminGroupResponse?> GetGroupAsync(Guid groupId, CancellationToken ct = default);
    Task<AdminGroupResponse?> CreateGroupAsync(CreateAdminGroupRequest request, CancellationToken ct = default);
    Task<AdminGroupResponse?> UpdateGroupAsync(Guid groupId, UpdateAdminGroupRequest request, CancellationToken ct = default);
    Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken ct = default);

    Task<object> GetAllHouseholdsAsync(CancellationToken ct = default);
    Task<AdminHouseholdResponse?> GetHouseholdAsync(Guid householdId, CancellationToken ct = default);
    Task<AdminHouseholdResponse?> CreateHouseholdAsync(CreateAdminHouseholdRequest request, CancellationToken ct = default);
    Task<AdminHouseholdResponse?> UpdateHouseholdAsync(Guid householdId, UpdateAdminHouseholdRequest request, CancellationToken ct = default);
    Task<bool> DeleteHouseholdAsync(Guid householdId, CancellationToken ct = default);
}
