using Mealie.Application.Dtos.Webhooks;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Webhooks;

public record GetEventNotifiersQuery(Guid HouseholdId) : IQuery<IList<EventNotifierResponse>>
{
    public async Task<IList<EventNotifierResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var notifiers = await services.Db.EventNotifiers.IgnoreQueryFilters()
            .Where(e => e.HouseholdId == HouseholdId).ToListAsync(ct);
        return notifiers.Select(NotifierMappings.MapToResponse).ToList();
    }
}

file static class NotifierMappings
{
    public static EventNotifierResponse MapToResponse(EventNotifier e) =>
        new() { Id = e.Id, Name = e.Name, ApprisUrl = e.ApprisUrl, Enabled = e.Enabled };
}
