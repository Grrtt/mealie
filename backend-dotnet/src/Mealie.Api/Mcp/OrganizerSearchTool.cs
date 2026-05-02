using System.ComponentModel;
using System.Text.Json;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace Mealie.Api.Mcp;

[McpServerToolType]
public class OrganizerSearchTool(ApplicationDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    [McpServerTool(Name = "search_categories")]
    [Description("Search recipe categories by name with pagination.")]
    public async Task<string> SearchCategories(
        [Description("Text to match against category name (case-insensitive contains).")]
        string? search,
        [Description("1-based page number. Defaults to 1.")]
        int page = 1,
        [Description("Number of results per page. Defaults to 50.")]
        int perPage = 50)
    {
        var query = db.Categories
            .IgnoreQueryFilters()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(lower));
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)perPage);
        var skip = (page - 1) * perPage;

        var categories = await query
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(perPage)
            .ToListAsync();

        var items = categories.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            slug = c.Slug,
            groupId = c.GroupId,
        });

        return JsonSerializer.Serialize(new { total, page, perPage, totalPages, items }, JsonOptions);
    }

    [McpServerTool(Name = "search_tags")]
    [Description("Search recipe tags by name with pagination.")]
    public async Task<string> SearchTags(
        [Description("Text to match against tag name (case-insensitive contains).")]
        string? search,
        [Description("1-based page number. Defaults to 1.")]
        int page = 1,
        [Description("Number of results per page. Defaults to 50.")]
        int perPage = 50)
    {
        var query = db.Tags
            .IgnoreQueryFilters()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(lower));
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)perPage);
        var skip = (page - 1) * perPage;

        var tags = await query
            .OrderBy(t => t.Name)
            .Skip(skip)
            .Take(perPage)
            .ToListAsync();

        var items = tags.Select(t => new
        {
            id = t.Id,
            name = t.Name,
            slug = t.Slug,
            groupId = t.GroupId,
        });

        return JsonSerializer.Serialize(new { total, page, perPage, totalPages, items }, JsonOptions);
    }
}
