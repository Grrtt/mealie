using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Entities.Recipes;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    private readonly TenantFilter _tenantFilter;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TenantFilter tenantFilter)
        : base(options)
    {
        _tenantFilter = tenantFilter;
    }

    // Core
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    // Recipes
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeInstruction> RecipeInstructions => Set<RecipeInstruction>();
    public DbSet<RecipeNote> RecipeNotes => Set<RecipeNote>();
    public DbSet<RecipeAsset> RecipeAssets => Set<RecipeAsset>();
    public DbSet<RecipeComment> RecipeComments => Set<RecipeComment>();
    public DbSet<RecipeTimelineEvent> RecipeTimelineEvents => Set<RecipeTimelineEvent>();
    public DbSet<RecipeShareToken> RecipeShareTokens => Set<RecipeShareToken>();

    // Organizers
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tool> Tools => Set<Tool>();
    public DbSet<Cookbook> Cookbooks => Set<Cookbook>();
    public DbSet<MultiPurposeLabel> Labels => Set<MultiPurposeLabel>();
    public DbSet<GroupInviteToken> InviteTokens => Set<GroupInviteToken>();

    // Ingredients
    public DbSet<IngredientFood> Foods => Set<IngredientFood>();
    public DbSet<IngredientUnit> Units => Set<IngredientUnit>();
    public DbSet<IngredientFoodAlias> FoodAliases => Set<IngredientFoodAlias>();

    // Planning
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<ShoppingListItemRecipeReference> ShoppingListItemRecipeReferences => Set<ShoppingListItemRecipeReference>();
    public DbSet<ShoppingListRecipeReference> ShoppingListRecipeReferences => Set<ShoppingListRecipeReference>();

    // Settings
    public DbSet<GroupPreferences> GroupPreferences => Set<GroupPreferences>();
    public DbSet<HouseholdPreferences> HouseholdPreferences => Set<HouseholdPreferences>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<EventNotifier> EventNotifiers => Set<EventNotifier>();
    public DbSet<ServerTask> ServerTasks => Set<ServerTask>();

    // Reports
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportEntry> ReportEntries => Set<ReportEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Guid>().HaveConversion<UppercaseGuidConverter>();
        configurationBuilder.Properties<Guid?>().HaveConversion<UppercaseGuidConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Household-scoped query filters — pass Guid.Empty to bypass (admin)
        modelBuilder.Entity<Recipe>().HasQueryFilter(r => _tenantFilter.HouseholdId == Guid.Empty || r.HouseholdId == _tenantFilter.HouseholdId);
        modelBuilder.Entity<MealPlan>().HasQueryFilter(m => _tenantFilter.HouseholdId == Guid.Empty || m.HouseholdId == _tenantFilter.HouseholdId);
        modelBuilder.Entity<ShoppingList>().HasQueryFilter(s => _tenantFilter.HouseholdId == Guid.Empty || s.HouseholdId == _tenantFilter.HouseholdId);
        modelBuilder.Entity<Cookbook>().HasQueryFilter(c => _tenantFilter.HouseholdId == Guid.Empty || c.HouseholdId == _tenantFilter.HouseholdId);
        modelBuilder.Entity<Webhook>().HasQueryFilter(w => _tenantFilter.HouseholdId == Guid.Empty || w.HouseholdId == _tenantFilter.HouseholdId);
        modelBuilder.Entity<EventNotifier>().HasQueryFilter(e => _tenantFilter.HouseholdId == Guid.Empty || e.HouseholdId == _tenantFilter.HouseholdId);

        // Group-scoped query filters
        modelBuilder.Entity<Tag>().HasQueryFilter(t => _tenantFilter.GroupId == Guid.Empty || t.GroupId == _tenantFilter.GroupId);
        modelBuilder.Entity<Category>().HasQueryFilter(c => _tenantFilter.GroupId == Guid.Empty || c.GroupId == _tenantFilter.GroupId);
        modelBuilder.Entity<Tool>().HasQueryFilter(t => _tenantFilter.GroupId == Guid.Empty || t.GroupId == _tenantFilter.GroupId);
        modelBuilder.Entity<IngredientFood>().HasQueryFilter(f => _tenantFilter.GroupId == Guid.Empty || f.GroupId == _tenantFilter.GroupId);
        modelBuilder.Entity<IngredientUnit>().HasQueryFilter(u => _tenantFilter.GroupId == Guid.Empty || u.GroupId == _tenantFilter.GroupId);
        modelBuilder.Entity<MultiPurposeLabel>().HasQueryFilter(l => _tenantFilter.GroupId == Guid.Empty || l.GroupId == _tenantFilter.GroupId);
    }
}
