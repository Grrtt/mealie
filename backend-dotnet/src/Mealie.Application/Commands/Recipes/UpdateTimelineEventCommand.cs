using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Services.Images;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record UpdateTimelineEventCommand(Guid EventId, UpdateTimelineEventRequest Request) : IQuery<TimelineEventResponse?>
{
    public async Task<TimelineEventResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var ev = await db.RecipeTimelineEvents.FindAsync([EventId], ct);
        if (ev is null) return null;
        ev.Subject = Request.Subject ?? ev.Subject;
        ev.EventType = Request.EventType ?? ev.EventType;
        ev.EventMessage = Request.EventMessage ?? ev.EventMessage;
        ev.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return TimelineMappings.MapToResponse(ev);
    }
}

file static class TimelineMappings
{
    public static TimelineEventResponse MapToResponse(RecipeTimelineEvent ev) => new()
    {
        Id = ev.Id, Subject = ev.Subject, EventType = ev.EventType, EventMessage = ev.EventMessage,
        Image = ev.Image, RecipeId = ev.RecipeId, UserId = ev.UserId, Timestamp = ev.Timestamp,
        CreatedAt = ev.CreatedAt, UpdateAt = ev.UpdateAt
    };
}