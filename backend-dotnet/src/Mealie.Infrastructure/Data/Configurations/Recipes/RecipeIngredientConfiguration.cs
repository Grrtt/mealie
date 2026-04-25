using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Recipes;

public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("recipes_ingredients");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.RecipeId);
        builder.HasOne(i => i.Recipe)
               .WithMany(r => r.RecipeIngredients)
               .HasForeignKey(i => i.RecipeId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Unit)
               .WithMany()
               .HasForeignKey(i => i.UnitId)
               .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(i => i.Food)
               .WithMany()
               .HasForeignKey(i => i.FoodId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
