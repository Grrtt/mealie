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
            .Select(e => new TimelineEventResponse
            {
                Id = e.Id,
                Subject = e.Subject,
                EventType = e.EventType,
                EventMessage = e.EventMessage,
                Image = e.Image,
                RecipeId = e.RecipeId,
                UserId = e.UserId,
                Timestamp = e.Timestamp,
                CreatedAt = e.CreatedAt,
                UpdateAt = e.UpdateAt,
                RecipeName = e.Recipe.Name,
                RecipeSlug = e.Recipe.Slug,
                RecipeImage = e.Recipe.Image,
                RecipeDescription = e.Recipe.Description,
                RecipeTotalTime = e.Recipe.TotalTime,
                RecipeRating = e.Recipe.Rating
            })
            .ToListAsync(ct);

        return new { items, total, page = Page, perPage = PerPage };
    }
}
