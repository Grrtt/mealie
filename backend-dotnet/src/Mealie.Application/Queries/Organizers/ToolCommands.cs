using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries.Shared;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetToolsQuery(Guid GroupId, PaginationParams Pagination, string? Search = null)
    : IQuery<PaginatedResponse<ToolResponse>>
{
    public async Task<PaginatedResponse<ToolResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.Tools.IgnoreQueryFilters().Where(t => t.GroupId == GroupId);
        if (!string.IsNullOrWhiteSpace(Search))
            query = query.Where(t => t.Name.Contains(Search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.Name)
            .Skip(Pagination.Skip).Take(Pagination.PerPage)
            .Select(t => new ToolResponse { Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, OnHand = t.OnHand, CreatedAt = t.CreatedAt, UpdateAt = t.UpdateAt })
            .ToListAsync(ct);
        return new PaginatedResponse<ToolResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage), Items = items
        };
    }
}

public record GetToolBySlugQuery(Guid GroupId, string Slug) : IQuery<ToolResponse?>
{
    public async Task<ToolResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var t = await services.Db.Tools.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Slug == Slug, ct);
        if (t is null) return null;
        return new ToolResponse { Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, OnHand = t.OnHand, CreatedAt = t.CreatedAt, UpdateAt = t.UpdateAt };
    }
}

public record CreateToolCommand(Guid GroupId, CreateToolRequest Request) : IQuery<ToolResponse>
{
    public async Task<ToolResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var slug = await ToolSlugHelper.EnsureUniqueAsync(db, SlugHelper.Generate(Request.Name), GroupId, ct);
        var tool = new Tool { Id = Guid.NewGuid(), Name = Request.Name, Slug = slug, GroupId = GroupId, OnHand = Request.OnHand, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
        db.Tools.Add(tool);
        await db.SaveChangesAsync(ct);
        return new ToolResponse { Id = tool.Id, Name = tool.Name, Slug = tool.Slug, GroupId = tool.GroupId, OnHand = tool.OnHand, CreatedAt = tool.CreatedAt, UpdateAt = tool.UpdateAt };
    }
}

public record UpdateToolCommand(Guid GroupId, Guid Id, UpdateToolRequest Request) : IQuery<ToolResponse?>
{
    public async Task<ToolResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var tool = await db.Tools.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == Id, ct);
        if (tool is null) return null;
        if (Request.Name is not null) { tool.Name = Request.Name; tool.Slug = SlugHelper.Generate(Request.Name); }
        if (Request.OnHand.HasValue) tool.OnHand = Request.OnHand.Value;
        tool.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new ToolResponse { Id = tool.Id, Name = tool.Name, Slug = tool.Slug, GroupId = tool.GroupId, OnHand = tool.OnHand, CreatedAt = tool.CreatedAt, UpdateAt = tool.UpdateAt };
    }
}

public record DeleteToolCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var tool = await db.Tools.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == Id, ct);
        if (tool is null) return false;
        db.Tools.Remove(tool);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record GetRecipesByToolQuery(Guid GroupId, Guid ToolId) : IQuery<IList<RecipeSummaryResponse>>
{
    public async Task<IList<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var tool = await services.Db.Tools.IgnoreQueryFilters()
            .Include(t => t.Recipes).ThenInclude(r => r.Tags)
            .Include(t => t.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == ToolId, ct);
        if (tool is null) return [];
        return tool.Recipes.Select(RecipeMappings.MapToSummary).ToList();
    }
}

file static class ToolSlugHelper
{
    public static async Task<string> EnsureUniqueAsync(ApplicationDbContext db, string slug, Guid groupId, CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Tools.IgnoreQueryFilters().AnyAsync(t => t.GroupId == groupId && t.Slug == candidate, ct))
            candidate = $"{slug}-{counter++}";
        return candidate;
    }
}
