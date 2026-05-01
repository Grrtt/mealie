using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Mealie.Application.Queries;
namespace Mealie.Application.Commands.Organizers;

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
