using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Images;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Recipes;

public class RecipeTimelineService(ApplicationDbContext db) : IRecipeTimelineService
{
    public async Task<object> GetAllEventsAsync(Guid groupId, int page, int perPage, CancellationToken ct = default)
    {
        var query = db.RecipeTimelineEvents
            .IgnoreQueryFilters()
            .Where(e => db.Recipes.Any(r => r.Id == e.RecipeId && r.GroupId == groupId))
            .OrderByDescending(e => e.Timestamp);

        var total = await query.CountAsync(ct);
        var skip = perPage > 0 ? (page - 1) * perPage : 0;
        var take = perPage > 0 ? perPage : total;

        var items = await query
            .Skip(skip)
            .Take(take)
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
                UpdateAt = e.UpdateAt
            })
            .ToListAsync(ct);

        return new { items, total, page, perPage };
    }

    public async Task<IList<TimelineEventResponse>> GetEventsAsync(string slug, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == slug, ct);
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
                UpdateAt = e.UpdateAt
            })
            .ToListAsync(ct);
    }

    public async Task<TimelineEventResponse?> AddEventAsync(string slug, Guid userId,
        CreateTimelineEventRequest request, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (recipe is null)
        {
            return null;
        }

        var ev = new RecipeTimelineEvent
        {
            Id = Guid.NewGuid(),
            Subject = request.Subject,
            EventType = request.EventType,
            EventMessage = request.EventMessage,
            RecipeId = recipe.Id,
            UserId = userId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        db.RecipeTimelineEvents.Add(ev);
        await db.SaveChangesAsync(ct);

        return MapToResponse(ev);
    }

    public async Task<TimelineEventResponse?> UpdateEventAsync(Guid eventId, UpdateTimelineEventRequest request,
        CancellationToken ct = default)
    {
        var ev = await db.RecipeTimelineEvents.FindAsync([eventId], ct);
        if (ev is null)
        {
            return null;
        }

        ev.Subject = request.Subject ?? ev.Subject;
        ev.EventType = request.EventType ?? ev.EventType;
        ev.EventMessage = request.EventMessage ?? ev.EventMessage;
        ev.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return MapToResponse(ev);
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var ev = await db.RecipeTimelineEvents.FindAsync([eventId], ct);
        if (ev is null)
        {
            return false;
        }

        db.RecipeTimelineEvents.Remove(ev);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TimelineEventResponse?> UploadImageAsync(Guid eventId, byte[] imageBytes, string dataDir,
        CancellationToken ct = default)
    {
        var ev = await db.RecipeTimelineEvents.FindAsync([eventId], ct);
        if (ev is null)
        {
            return null;
        }

        var dir = Path.Combine(dataDir, "recipes", ev.RecipeId.ToString(), "images", "timeline", ev.Id.ToString());
        RecipeImageProcessor.SaveVariants(dir, imageBytes);

        ev.Image = "original.webp";
        ev.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return MapToResponse(ev);
    }

    private static TimelineEventResponse MapToResponse(RecipeTimelineEvent ev)
    {
        return new TimelineEventResponse
        {
            Id = ev.Id,
            Subject = ev.Subject,
            EventType = ev.EventType,
            EventMessage = ev.EventMessage,
            Image = ev.Image,
            RecipeId = ev.RecipeId,
            UserId = ev.UserId,
            Timestamp = ev.Timestamp,
            CreatedAt = ev.CreatedAt,
            UpdateAt = ev.UpdateAt
        };
    }
}
