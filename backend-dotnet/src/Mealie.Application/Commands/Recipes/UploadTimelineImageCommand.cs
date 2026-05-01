using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Services.Images;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

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