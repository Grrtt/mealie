using Mealie.Application.Dtos.Recipes;

namespace Mealie.Application.Services.Recipes;

public interface IRecipeTimelineService
{
    Task<object> GetAllEventsAsync(Guid groupId, int page, int perPage, CancellationToken ct = default);
    Task<IList<TimelineEventResponse>> GetEventsAsync(string slug, CancellationToken ct = default);

    Task<TimelineEventResponse?> AddEventAsync(string slug, Guid userId, CreateTimelineEventRequest request,
        CancellationToken ct = default);

    Task<TimelineEventResponse?> UpdateEventAsync(Guid eventId, UpdateTimelineEventRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteEventAsync(Guid eventId, CancellationToken ct = default);
}
