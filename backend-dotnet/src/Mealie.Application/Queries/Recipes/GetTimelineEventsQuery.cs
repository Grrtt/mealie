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
                RecipeName = recipe.Name,
                RecipeSlug = recipe.Slug,
                RecipeImage = recipe.Image,
                RecipeDescription = recipe.Description,
                RecipeTotalTime = recipe.TotalTime,
                RecipeRating = recipe.Rating
            })
            .ToListAsync(ct);
    }
}
