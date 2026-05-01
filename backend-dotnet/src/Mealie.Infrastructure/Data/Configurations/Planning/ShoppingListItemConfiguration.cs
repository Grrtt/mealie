using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Planning;

public class ShoppingListItemConfiguration : IEntityTypeConfiguration<ShoppingListItem>
{
    public void Configure(EntityTypeBuilder<ShoppingListItem> builder)
    {
        builder.ToTable("shopping_list_items");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.ShoppingListId);
        builder.HasOne(i => i.ShoppingList)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Unit)
            .WithMany()
            .HasForeignKey(i => i.UnitId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(i => i.Food)
            .WithMany()
            .HasForeignKey(i => i.FoodId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(i => i.Label)
            .WithMany(l => l.ShoppingItems)
            .HasForeignKey(i => i.LabelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
