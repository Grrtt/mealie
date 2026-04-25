using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Entities.Settings;

namespace Mealie.Domain.Entities.Core;

public class Household
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public ICollection<User> Users { get; set; } = [];
    public HouseholdPreferences? Preferences { get; set; }
    public ICollection<Cookbook> Cookbooks { get; set; } = [];
    public ICollection<MealPlan> MealPlans { get; set; } = [];
    public ICollection<ShoppingList> ShoppingLists { get; set; } = [];
    public ICollection<Webhook> Webhooks { get; set; } = [];
    public ICollection<EventNotifier> EventNotifiers { get; set; } = [];
}
