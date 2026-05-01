using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Images;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record AddTimelineEventCommand(string Slug, Guid UserId, CreateTimelineEventRequest Request) : IQuery<TimelineEventResponse?>
{
    public async Task<TimelineEventResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null) return null;
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

public record DeleteTimelineEventCommand(Guid EventId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var ev = await db.RecipeTimelineEvents.FindAsync([EventId], ct);
        if (ev is null) return false;
        db.RecipeTimelineEvents.Remove(ev);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record UploadTimelineImageCommand(Guid EventId, byte[] ImageBytes) : IQuery<TimelineEventResponse?>
{
    public async Task<TimelineEventResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var ev = await db.RecipeTimelineEvents.FindAsync([EventId], ct);
        if (ev is null) return null;
        var dir = Path.Combine(services.Settings.Value.DataDir, "recipes", ev.RecipeId.ToString(), "images", "timeline", ev.Id.ToString());
        RecipeImageProcessor.SaveVariants(dir, ImageBytes);
        ev.Image = "original.webp";
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
