using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mealie.Application.Dtos.RecipeActions;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.RecipeActions;

public record TriggerRecipeActionCommand(
    Guid HouseholdId,
    Guid GroupId,
    Guid ActionId,
    string RecipeSlug,
    RecipeActionTriggerRequest? Request) : IQuery<RecipeActionTriggerResponse?>
{
    public async Task<RecipeActionTriggerResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;

        var action = await db.RecipeActions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.HouseholdId == HouseholdId && a.Id == ActionId, ct);
        if (action is null)
        {
            return null;
        }

        var recipe = await db.Recipes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.HouseholdId == HouseholdId && r.GroupId == GroupId && r.Slug == RecipeSlug, ct);
        if (recipe is null)
        {
            return null;
        }

        var substitutedUrl = SubstituteVariables(action.Url, recipe, Request);

        if (action.ActionType.Equals("link", StringComparison.OrdinalIgnoreCase))
        {
            return new RecipeActionTriggerResponse { Url = substitutedUrl, Triggered = false };
        }

        // "post" type: send recipe data as JSON
        var recipeData = new
        {
            recipe.Id,
            recipe.Slug,
            recipe.Name,
            recipe.Description,
            recipe.RecipeYield,
            recipe.TotalTime,
            recipe.PrepTime,
            recipe.CookTime,
            recipe.PerformTime,
            recipe.Rating,
            recipe.Image,
            recipe.OrgUrl,
            recipe.CreatedAt,
            recipe.UpdateAt,
            recipeScaling = Request?.RecipeScale
        };

        try
        {
            var httpClient = services.HttpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var response = await httpClient.PostAsJsonAsync(substitutedUrl, recipeData, ct);
            return new RecipeActionTriggerResponse
            {
                Url = substitutedUrl,
                Triggered = true,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception)
        {
            return new RecipeActionTriggerResponse
            {
                Url = substitutedUrl,
                Triggered = true,
                StatusCode = 0
            };
        }
    }

    private static string SubstituteVariables(string template, Domain.Entities.Recipes.Recipe recipe,
        RecipeActionTriggerRequest? request)
    {
        var (yieldQuantity, yieldText) = ParseYield(recipe.RecipeYield);

        return template
            .Replace("${url}", recipe.OrgUrl ?? "")
            .Replace("${id}", recipe.Id.ToString())
            .Replace("${slug}", recipe.Slug)
            .Replace("${servings}", yieldQuantity)
            .Replace("${yieldQuantity}", yieldQuantity)
            .Replace("${yieldText}", yieldText)
            .Replace("${recipeScale}", request?.RecipeScale?.ToString() ?? "");
    }

    private static (string quantity, string text) ParseYield(string? recipeYield)
    {
        if (string.IsNullOrWhiteSpace(recipeYield))
        {
            return ("", "");
        }

        var match = Regex.Match(recipeYield, @"^(\d+)(?:\s+(.*))?$");
        if (match.Success)
        {
            return (match.Groups[1].Value, match.Groups[2].Value.Trim());
        }

        return ("", recipeYield.Trim());
    }
}
