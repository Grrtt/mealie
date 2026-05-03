using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Planning;

public class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList>
{
    public void Configure(EntityTypeBuilder<ShoppingList> builder)
    {
        builder.ToTable("shopping_lists");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.GroupId);
        builder.HasIndex(s => s.HouseholdId);
        builder.HasIndex(s => s.UserId);
        builder.Property(s => s.Name).IsRequired();
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Group)
            .WithMany(g => g.ShoppingLists)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Household)
            .WithMany(h => h.ShoppingLists)
            .HasForeignKey(s => s.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
