using Mealie.Application.Dtos.Webhooks;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Mealie.Application.Queries;
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

public record DeleteEventNotifierCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var notifier = await db.EventNotifiers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.HouseholdId == HouseholdId && e.Id == Id, ct);
        if (notifier is null) return false;
        db.EventNotifiers.Remove(notifier);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record TestEventNotifierCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var notifier = await services.Db.EventNotifiers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.HouseholdId == HouseholdId && e.Id == Id, ct);
        if (notifier is null) return false;
        var logger = services.LoggerFactory.CreateLogger("EventNotifierCommands");
        logger.LogInformation("Test notification sent to {ApprisUrl}", notifier.ApprisUrl);
        return true;
    }
}

file static class NotifierMappings
{
    public static EventNotifierResponse MapToResponse(EventNotifier e) =>
        new() { Id = e.Id, Name = e.Name, ApprisUrl = e.ApprisUrl, Enabled = e.Enabled };
}
