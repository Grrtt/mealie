using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record GetAllTimelineEventsQuery(Guid GroupId, int Page, int PerPage) : IQuery<object>
{
    public async Task<object> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.RecipeTimelineEvents
            .IgnoreQueryFilters()
            .Where(e => db.Recipes.Any(r => r.Id == e.RecipeId && r.GroupId == GroupId))
            .OrderByDescending(e => e.Timestamp);

        var total = await query.CountAsync(ct);
        var skip = PerPage > 0 ? (Page - 1) * PerPage : 0;
        var take = PerPage > 0 ? PerPage : total;

        var items = await query.Skip(skip).Take(take)
            .Select(e => TimelineMappings.MapToResponse(e))
            .ToListAsync(ct);

        return new { items, total, page = Page, perPage = PerPage };
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
