using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Planning;

public class ShoppingListRecipeReferenceConfiguration : IEntityTypeConfiguration<ShoppingListRecipeReference>
{
    public void Configure(EntityTypeBuilder<ShoppingListRecipeReference> builder)
    {
        // Python table name: shopping_list_recipe_reference (singular)
        builder.ToTable("shopping_list_recipe_reference");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.ShoppingListId);
        builder.HasOne(r => r.ShoppingList)
            .WithMany(s => s.RecipeReferences)
            .HasForeignKey(r => r.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Recipe)
            .WithMany()
            .HasForeignKey(r => r.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
