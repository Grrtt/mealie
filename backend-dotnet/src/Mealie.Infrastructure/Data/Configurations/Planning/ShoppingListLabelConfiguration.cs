using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Planning;

public class ShoppingListLabelConfiguration : IEntityTypeConfiguration<ShoppingListLabel>
{
    public void Configure(EntityTypeBuilder<ShoppingListLabel> builder)
    {
        builder.ToTable("shopping_list_labels");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.ShoppingListId);
        builder.HasIndex(l => l.LabelId);
        builder.HasIndex(l => new { l.ShoppingListId, l.Position });
        builder.Property(l => l.Position).IsRequired();

        builder.HasOne(l => l.ShoppingList)
            .WithMany(s => s.Labels)
            .HasForeignKey(l => l.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Label)
            .WithMany(lb => lb.ShoppingListLabels)
            .HasForeignKey(l => l.LabelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
