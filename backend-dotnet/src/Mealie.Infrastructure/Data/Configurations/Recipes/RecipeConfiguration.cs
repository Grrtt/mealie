using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Recipes;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.Slug, r.GroupId }).IsUnique();
        builder.HasIndex(r => r.GroupId);
        builder.HasIndex(r => r.HouseholdId);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(255);
        builder.Property(r => r.Slug).IsRequired().HasMaxLength(255);

        // Owned value objects — columns embedded in recipes table
        builder.OwnsOne(r => r.Nutrition, n =>
        {
            n.Property(x => x.Calories).HasColumnName("nutrition_calories");
            n.Property(x => x.FatContent).HasColumnName("nutrition_fat_content");
            n.Property(x => x.ProteinContent).HasColumnName("nutrition_protein_content");
            n.Property(x => x.CarbohydrateContent).HasColumnName("nutrition_carbohydrate_content");
            n.Property(x => x.FiberContent).HasColumnName("nutrition_fiber_content");
            n.Property(x => x.SodiumContent).HasColumnName("nutrition_sodium_content");
            n.Property(x => x.SugarContent).HasColumnName("nutrition_sugar_content");
        });

        builder.OwnsOne(r => r.Settings, s =>
        {
            s.Property(x => x.Public).HasColumnName("settings_public");
            s.Property(x => x.ShowNutrition).HasColumnName("settings_show_nutrition");
            s.Property(x => x.ShowAssets).HasColumnName("settings_show_assets");
            s.Property(x => x.LandscapeView).HasColumnName("settings_landscape_view");
            s.Property(x => x.DisableComments).HasColumnName("settings_disable_comments");
            s.Property(x => x.DisableAmount).HasColumnName("settings_disable_amount");
            s.Property(x => x.Locked).HasColumnName("settings_locked");
        });

        // Many-to-many with junction tables — column names must match the Python SQLAlchemy source
        builder.HasMany(r => r.Tags)
            .WithMany(t => t.Recipes)
            .UsingEntity<Dictionary<string, object>>(
                "recipes_to_tags",
                j => j.HasOne<Tag>().WithMany().HasForeignKey("tag_id"),
                j => j.HasOne<Recipe>().WithMany().HasForeignKey("recipe_id"),
                j => j.HasKey("recipe_id", "tag_id"));
        builder.HasMany(r => r.Categories)
            .WithMany(c => c.Recipes)
            .UsingEntity<Dictionary<string, object>>(
                "recipes_to_categories",
                j => j.HasOne<Category>().WithMany().HasForeignKey("category_id"),
                j => j.HasOne<Recipe>().WithMany().HasForeignKey("recipe_id"),
                j => j.HasKey("category_id", "recipe_id"));
        builder.HasMany(r => r.Tools)
            .WithMany(t => t.Recipes)
            .UsingEntity<Dictionary<string, object>>(
                "recipes_to_tools",
                j => j.HasOne<Tool>().WithMany().HasForeignKey("tool_id"),
                j => j.HasOne<Recipe>().WithMany().HasForeignKey("recipe_id"),
                j => j.HasKey("recipe_id", "tool_id"));

        builder.HasOne(r => r.Group)
            .WithMany(g => g.Recipes)
            .HasForeignKey(r => r.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Household)
            .WithMany()
            .HasForeignKey(r => r.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
