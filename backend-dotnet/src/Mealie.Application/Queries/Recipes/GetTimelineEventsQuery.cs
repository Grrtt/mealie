using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record GetTimelineEventsQuery(string Slug) : IQuery<IList<TimelineEventResponse>>
{
    public async Task<IList<TimelineEventResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null)
        {
            return [];
        }

        return await db.RecipeTimelineEvents
            .Where(e => e.RecipeId == recipe.Id)
            .OrderByDescending(e => e.Timestamp)
            .Select(e => TimelineMappings.MapToResponse(e))
            .ToListAsync(ct);
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
