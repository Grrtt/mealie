namespace Mealie.Application.Dtos.ShoppingLists;

public class ShoppingListResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public IList<ShoppingListItemResponse> Items { get; set; } = [];
}

public class ShoppingListItemResponse
{
    public Guid Id { get; set; }
    public string? Note { get; set; }
    public bool IsFood { get; set; }
    public bool Checked { get; set; }
    public bool DisableAmount { get; set; }
    public decimal? Quantity { get; set; }
    public Guid ShoppingListId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? FoodId { get; set; }
    public Guid? LabelId { get; set; }
    public int Position { get; set; }
    public string? UnitName { get; set; }
    public string? FoodName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class ShoppingListSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class CreateShoppingListRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateShoppingListRequest
{
    public string? Name { get; set; }
}

public class CreateShoppingListItemRequest
{
    public string? Note { get; set; }
    public bool IsFood { get; set; }
    public bool DisableAmount { get; set; }
    public decimal? Quantity { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? FoodId { get; set; }
    public Guid? LabelId { get; set; }
}

public class UpdateShoppingListItemRequest
{
    public string? Note { get; set; }
    public bool? Checked { get; set; }
    public bool? DisableAmount { get; set; }
    public decimal? Quantity { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? FoodId { get; set; }
    public Guid? LabelId { get; set; }
}
