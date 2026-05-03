using Mealie.Application.Common;
using Mealie.Application.Services.Parser;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

/// <summary>
///     Shared helpers used by both the scrape-from-URL and re-import code paths so that
///     ingredient formatting, tag/category resolution, and AI organizer calls are
///     implemented exactly once.
/// </summary>
internal static class RecipeImportHelpers
{
    /// <summary>
    ///     Formats a parsed ingredient result into a human-readable string
    ///     (the same representation shown to the user in the ingredient parser dialog).
    /// </summary>
    public static string FormatParsedIngredientDto(ParsedIngredientDto dto)
    {
        var food = dto.Ingredient.Food?.Name;
        if (food is null) return dto.Input ?? string.Empty;

        var parts = new List<string>();
        if (dto.Ingredient.Quantity.HasValue)
            parts.Add(dto.Ingredient.Quantity.Value % 1 == 0
                ? ((int)dto.Ingredient.Quantity.Value).ToString()
                : dto.Ingredient.Quantity.Value.ToString("G"));
        if (!string.IsNullOrWhiteSpace(dto.Ingredient.Unit?.Name))
            parts.Add(dto.Ingredient.Unit.Name);
        parts.Add(food);
        if (!string.IsNullOrWhiteSpace(dto.Ingredient.Note))
            parts.Add($"({dto.Ingredient.Note})");
        return string.Join(" ", parts);
    }

    /// <summary>
    ///     Builds an organizer context and calls the AI organizer service.
    ///     Returns null silently if no AI provider is configured.
    /// </summary>
    public static Task<RecipeOrganizerSuggestions?> SuggestOrganizersAsync(
        IRecipeOrganizerService organizer,
        string recipeName,
        string? description,
        IList<ParsedIngredientDto> parsed,
        IList<string> websiteCategories,
        IList<string> websiteTags,
        CancellationToken ct)
    {
        var ingredientDescriptions = parsed
            .Select(FormatParsedIngredientDto)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        return organizer.SuggestAsync(
            null,
            new RecipeOrganizerContext(
                RecipeName: recipeName,
                Description: description,
                Ingredients: ingredientDescriptions,
                WebsiteCategories: websiteCategories,
                WebsiteTags: websiteTags),
            ct);
    }

    /// <summary>
    ///     Finds or creates <see cref="Tag" /> entities for the given names (case-insensitive slug match).
    /// </summary>
    public static async Task<List<Tag>> ResolveTagsAsync(
        ApplicationDbContext db,
        Guid groupId,
        IEnumerable<string> names,
        CancellationToken ct)
    {
        var result = new List<Tag>();
        foreach (var name in names
                     .Select(n => n.Trim())
                     .Where(n => !string.IsNullOrEmpty(n))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var slug = SlugHelper.Generate(name);
            var tag = await db.Tags.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == slug && t.GroupId == groupId, ct);
            if (tag is null)
            {
                tag = new Tag
                {
                    Id = Guid.NewGuid(), Name = name, Slug = slug, GroupId = groupId,
                    CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                };
                db.Tags.Add(tag);
            }

            result.Add(tag);
        }

        return result;
    }

    /// <summary>
    ///     Finds or creates <see cref="Category" /> entities for the given names (case-insensitive slug match).
    /// </summary>
    public static async Task<List<Category>> ResolveCategoriesAsync(
        ApplicationDbContext db,
        Guid groupId,
        IEnumerable<string> names,
        CancellationToken ct)
    {
        var result = new List<Category>();
        foreach (var name in names
                     .Select(n => n.Trim())
                     .Where(n => !string.IsNullOrEmpty(n))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var slug = SlugHelper.Generate(name);
            var cat = await db.Categories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Slug == slug && c.GroupId == groupId, ct);
            if (cat is null)
            {
                cat = new Category
                {
                    Id = Guid.NewGuid(), Name = name, Slug = slug, GroupId = groupId,
                    CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                };
                db.Categories.Add(cat);
            }

            result.Add(cat);
        }

        return result;
    }
}
