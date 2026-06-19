using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.ShoppingLists;

public static class ShoppingListMappingHelper
{
    public static IQueryable<ShoppingList> WithItems(IQueryable<ShoppingList> query)
    {
        return query.Include(s => s.Items).ThenInclude(i => i.Unit)
            .Include(s => s.Items).ThenInclude(i => i.Food)
            .Include(s => s.Labels).ThenInclude(l => l.Label);
    }

    public static IQueryable<ShoppingListItem> WithItemDetails(IQueryable<ShoppingListItem> query)
    {
        return query.Include(i => i.Unit).Include(i => i.Food);
    }

    public static ShoppingListResponse MapToResponse(ShoppingList list)
    {
        return new ShoppingListResponse
        {
            Id = list.Id,
            Name = list.Name,
            GroupId = list.GroupId,
            HouseholdId = list.HouseholdId,
            CreatedAt = list.CreatedAt,
            UpdateAt = list.UpdateAt,
            Items = list.Items.Select(MapItemToResponse).ToList(),
            LabelSettings = list.Labels
                .OrderBy(l => l.Position)
                .Select(l => new ShoppingListMultiPurposeLabelOut
                {
                    LabelId = l.LabelId,
                    Position = l.Position,
                    LabelName = l.Label.Name,
                    LabelColor = l.Label.Color
                })
                .ToList()
        };
    }

    public static ShoppingListItemResponse MapItemToResponse(ShoppingListItem item)
    {
        return new ShoppingListItemResponse
        {
            Id = item.Id,
            Note = item.Note,
            IsFood = item.IsFood,
            Checked = item.Checked,
            DisableAmount = item.DisableAmount,
            Quantity = item.Quantity,
            ShoppingListId = item.ShoppingListId,
            UnitId = item.UnitId,
            FoodId = item.FoodId,
            LabelId = item.LabelId,
            Position = item.Position,
            UnitName = item.Unit?.Name,
            FoodName = item.Food?.Name,
            CreatedAt = item.CreatedAt,
            UpdateAt = item.UpdateAt
        };
    }
}
