using Mealie.Domain.Entities.Recipes;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Recipes;

public sealed class RecipeTimelineCreationHandler(
    ApplicationDbContext db,
    ILogger<RecipeTimelineCreationHandler> logger) : INotificationHandler<RecipeCreatedEvent>
{
    private const string CreatedEventType = "system";
    private const string CreatedEventMessage = "Recipe created";

    public async Task Handle(RecipeCreatedEvent notification, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .Where(r => r.Id == notification.RecipeId)
            .Select(r => new { r.Id, r.Name, r.CreatedAt })
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
        {
            logger.LogWarning("Skipping recipe timeline creation for missing recipe {RecipeId}", notification.RecipeId);
            return;
        }

        db.RecipeTimelineEvents.Add(new RecipeTimelineEvent
        {
            Id = Guid.NewGuid(),
            Subject = recipe.Name,
            EventType = CreatedEventType,
            EventMessage = CreatedEventMessage,
            RecipeId = recipe.Id,
            UserId = notification.UserId,
            Timestamp = recipe.CreatedAt,
            CreatedAt = recipe.CreatedAt,
            UpdateAt = recipe.CreatedAt
        });

        await db.SaveChangesAsync(ct);
    }
}
