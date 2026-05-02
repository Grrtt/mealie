using System.ComponentModel;
using System.Text.Json;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace Mealie.Api.Mcp;

[McpServerToolType]
public class RecipeSearchTool(ApplicationDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    [McpServerTool(Name = "search_recipes")]
    [Description("Search recipes by text, tags, and/or categories with pagination.")]
    public async Task<string> SearchRecipes(
        [Description("Text to match against recipe name or description (case-insensitive).")]
        string? search,
        [Description("Filter by tag name or slug — any match qualifies.")]
        string[]? tags,
        [Description("Filter by category name or slug — any match qualifies.")]
        string[]? categories,
        [Description("1-based page number. Defaults to 1.")]
        int page = 1,
        [Description("Number of results per page. Defaults to 20.")]
        int perPage = 20)
    {
        var query = db.Recipes
            .IgnoreQueryFilters()
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(lower) ||
                (r.Description != null && r.Description.ToLower().Contains(lower)));
        }

        if (tags is { Length: > 0 })
        {
            query = query.Where(r => r.Tags.Any(t => tags.Contains(t.Name) || tags.Contains(t.Slug)));
        }

        if (categories is { Length: > 0 })
        {
            query = query.Where(r => r.Categories.Any(c => categories.Contains(c.Name) || categories.Contains(c.Slug)));
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)perPage);
        var skip = (page - 1) * perPage;

        var recipes = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(perPage)
            .ToListAsync();

        var items = recipes.Select(r => new
        {
            id = r.Id,
            name = r.Name,
            slug = r.Slug,
            description = r.Description,
            rating = r.Rating,
            householdId = r.HouseholdId,
            groupId = r.GroupId,
            tags = r.Tags.Select(t => new { id = t.Id, name = t.Name, slug = t.Slug }),
            categories = r.Categories.Select(c => new { id = c.Id, name = c.Name, slug = c.Slug }),
        });

        return JsonSerializer.Serialize(new { total, page, perPage, totalPages, items }, JsonOptions);
    }
}
