using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record AddTimelineEventCommand(string Slug, Guid UserId, CreateTimelineEventRequest Request)
    : IQuery<TimelineEventResponse?>
{
    public async Task<TimelineEventResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null)
        {
            return null;
        }

        var ev = new RecipeTimelineEvent
        {
            Id = Guid.NewGuid(), Subject = Request.Subject, EventType = Request.EventType,
            EventMessage = Request.EventMessage, RecipeId = recipe.Id, UserId = UserId,
            Timestamp = Request.Timestamp ?? DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.RecipeTimelineEvents.Add(ev);
        await db.SaveChangesAsync(ct);
        return TimelineMappings.MapToResponse(ev);
    }
}

file static class TimelineMappings
{
    public static TimelineEventResponse MapToResponse(RecipeTimelineEvent ev)
    {
        return new TimelineEventResponse
        {
            Id = ev.Id, Subject = ev.Subject, EventType = ev.EventType, EventMessage = ev.EventMessage,
            Image = ev.Image, RecipeId = ev.RecipeId, UserId = ev.UserId, Timestamp = ev.Timestamp,
            CreatedAt = ev.CreatedAt, UpdateAt = ev.UpdateAt
        };
    }
}
