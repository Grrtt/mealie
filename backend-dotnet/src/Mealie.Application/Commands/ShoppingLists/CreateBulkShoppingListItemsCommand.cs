using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record CreateBulkShoppingListItemsCommand(Guid HouseholdId, BulkCreateShoppingListItemRequest Request)
    : IQuery<IList<ShoppingListItemResponse>>
{
    public async Task<IList<ShoppingListItemResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var db = services.Db;
        var listIds = Request.Items.Select(i => i.ListId).Distinct().ToList();
        var lists = await db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == HouseholdId && listIds.Contains(s.Id)).ToListAsync(ct);
        if (lists.Count != listIds.Count)
        {
            throw new KeyNotFoundException("One or more shopping lists not found");
        }

        var responses = new List<ShoppingListItemResponse>();
        foreach (var req in Request.Items)
        {
            var itemRequest = new CreateShoppingListItemRequest
            {
                Note = req.Note, IsFood = req.IsFood, DisableAmount = req.DisableAmount,
                Quantity = req.Quantity, UnitId = req.UnitId, FoodId = req.FoodId, LabelId = req.LabelId
            };
            responses.Add(await ShoppingListItemMutationService.CreateItemAsync(db, req.ListId, itemRequest, true, ct));
        }

        return responses;
    }
}

