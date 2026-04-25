using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Entities.Recipes;
using Mealie.Domain.Entities.Settings;

namespace Mealie.Domain.Entities.Core;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public ICollection<Household> Households { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Recipe> Recipes { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Tool> Tools { get; set; } = [];
    public ICollection<IngredientUnit> Units { get; set; } = [];
    public ICollection<IngredientFood> Foods { get; set; } = [];
    public GroupPreferences? Preferences { get; set; }
    public ICollection<GroupInviteToken> InviteTokens { get; set; } = [];
    public ICollection<Webhook> Webhooks { get; set; } = [];
    public ICollection<Cookbook> Cookbooks { get; set; } = [];
    public ICollection<MealPlan> MealPlans { get; set; } = [];
    public ICollection<ShoppingList> ShoppingLists { get; set; } = [];
    public ICollection<MultiPurposeLabel> Labels { get; set; } = [];
}
