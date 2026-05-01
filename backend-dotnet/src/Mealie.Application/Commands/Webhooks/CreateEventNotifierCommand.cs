using Mealie.Application.Dtos.Webhooks;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Settings;

namespace Mealie.Application.Commands.Webhooks;

public record CreateEventNotifierCommand(Guid GroupId, Guid HouseholdId, CreateEventNotifierRequest Request)
    : IQuery<EventNotifierResponse>
{
    public async Task<EventNotifierResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var notifier = new EventNotifier
        {
            Id = Guid.NewGuid(), Name = Request.Name, ApprisUrl = Request.ApprisUrl,
            Enabled = Request.Enabled, GroupId = GroupId, HouseholdId = HouseholdId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.EventNotifiers.Add(notifier);
        await db.SaveChangesAsync(ct);
        return NotifierMappings.MapToResponse(notifier);
    }
}

file static class NotifierMappings
{
    public static EventNotifierResponse MapToResponse(EventNotifier e)
    {
        return new EventNotifierResponse { Id = e.Id, Name = e.Name, ApprisUrl = e.ApprisUrl, Enabled = e.Enabled };
    }
}
