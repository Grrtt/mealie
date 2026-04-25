using Mealie.Application.Dtos.Webhooks;

namespace Mealie.Application.Services.Webhooks;

public interface IEventNotifierService
{
    Task<IList<EventNotifierResponse>> GetAllAsync(Guid householdId, CancellationToken ct = default);
    Task<EventNotifierResponse> CreateAsync(Guid groupId, Guid householdId, CreateEventNotifierRequest request, CancellationToken ct = default);
    Task<EventNotifierResponse?> UpdateAsync(Guid householdId, Guid id, CreateEventNotifierRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default);
    Task TestAsync(Guid householdId, Guid id, CancellationToken ct = default);
}
