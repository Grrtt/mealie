using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

public class RecipeOrganizerService(
    IParserStrategyResolver resolver,
    ILogger<RecipeOrganizerService> logger) : IRecipeOrganizerService
{
    public async Task<RecipeOrganizerSuggestions?> SuggestAsync(
        string? parserKey,
        RecipeOrganizerContext context,
        CancellationToken ct = default)
    {
        var strategy = await resolver.ResolveAsync(parserKey, ct);
        if (strategy is not IOrganizerAiStrategy organizer)
        {
            logger.LogDebug(
                "Parser strategy {Strategy} does not support organizer suggestions, skipping",
                strategy.GetType().Name);
            return null;
        }

        return await organizer.SuggestOrganizersAsync(context, ct);
    }
}
