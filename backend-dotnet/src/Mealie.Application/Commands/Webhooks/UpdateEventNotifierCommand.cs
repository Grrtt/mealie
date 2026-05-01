using Mealie.Application.Dtos.Webhooks;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Commands.Webhooks;

public record UpdateEventNotifierCommand(Guid HouseholdId, Guid Id, CreateEventNotifierRequest Request)
    : IQuery<EventNotifierResponse?>
{
    public async Task<EventNotifierResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var notifier = await db.EventNotifiers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.HouseholdId == HouseholdId && e.Id == Id, ct);
        if (notifier is null) return null;
        notifier.Name = Request.Name;
        notifier.ApprisUrl = Request.ApprisUrl;
        notifier.Enabled = Request.Enabled;
        notifier.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NotifierMappings.MapToResponse(notifier);
    }
}

file static class NotifierMappings
{
    public static EventNotifierResponse MapToResponse(EventNotifier e) =>
        new() { Id = e.Id, Name = e.Name, ApprisUrl = e.ApprisUrl, Enabled = e.Enabled };
}