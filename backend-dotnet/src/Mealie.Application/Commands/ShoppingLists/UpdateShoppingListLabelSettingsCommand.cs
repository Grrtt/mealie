using System.Text.Json;
using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record UpdateShoppingListLabelSettingsCommand(
    Guid HouseholdId,
    Guid ListId,
    UpdateShoppingListLabelSettingsRequest Request)
    : IQuery<ShoppingListResponse?>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ShoppingListResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = await ShoppingListMappingHelper.WithItems(db.ShoppingLists.IgnoreQueryFilters())
            .Where(s => s.HouseholdId == HouseholdId && s.Id == ListId)
            .FirstOrDefaultAsync(ct);
        if (list is null)
        {
            return null;
        }

        if (Request.LabelSettings is not null)
        {
            var labelSettings = JsonSerializer.Deserialize<List<ShoppingListLabelSettingsItem>>(
                Request.LabelSettings, JsonOpts);

            if (labelSettings is not null)
            {
                // Remove existing label associations
                var existing = await db.ShoppingListLabels
                    .Where(l => l.ShoppingListId == ListId)
                    .ToListAsync(ct);
                db.ShoppingListLabels.RemoveRange(existing);

                // Create new label associations
                foreach (var item in labelSettings)
                {
                    db.ShoppingListLabels.Add(new ShoppingListLabel
                    {
                        Id = Guid.NewGuid(),
                        ShoppingListId = ListId,
                        LabelId = item.LabelId,
                        Position = item.Position
                    });
                }
            }
        }

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // Reload the list to get fresh Labels navigation data for the response
        var updated = await ShoppingListMappingHelper.WithItems(db.ShoppingLists.IgnoreQueryFilters())
            .Where(s => s.HouseholdId == HouseholdId && s.Id == ListId)
            .FirstOrDefaultAsync(ct);

        return updated is null ? null : ShoppingListMappingHelper.MapToResponse(updated);
    }
}
