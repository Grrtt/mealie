using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Planning;

public class ShoppingListItemRecipeReferenceConfiguration : IEntityTypeConfiguration<ShoppingListItemRecipeReference>
{
    public void Configure(EntityTypeBuilder<ShoppingListItemRecipeReference> builder)
    {
        // Python table name: shopping_list_item_recipe_reference (singular)
        builder.ToTable("shopping_list_item_recipe_reference");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.ShoppingListItemId);
        builder.HasOne(r => r.ShoppingListItem)
               .WithMany(i => i.RecipeReferences)
               .HasForeignKey(r => r.ShoppingListItemId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Recipe)
               .WithMany()
               .HasForeignKey(r => r.RecipeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
