using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Organizers;

public record UpdateToolCommand(Guid GroupId, Guid Id, UpdateToolRequest Request) : IQuery<ToolResponse?>
{
    public async Task<ToolResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var tool = await db.Tools.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == Id, ct);
        if (tool is null)
        {
            return null;
        }

        if (Request.Name is not null)
        {
            tool.Name = Request.Name;
            tool.Slug = SlugHelper.Generate(Request.Name);
        }

        if (Request.OnHand.HasValue)
        {
            tool.OnHand = Request.OnHand.Value;
        }

        tool.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new ToolResponse
        {
            Id = tool.Id, Name = tool.Name, Slug = tool.Slug, GroupId = tool.GroupId, OnHand = tool.OnHand,
            CreatedAt = tool.CreatedAt, UpdateAt = tool.UpdateAt
        };
    }
}
