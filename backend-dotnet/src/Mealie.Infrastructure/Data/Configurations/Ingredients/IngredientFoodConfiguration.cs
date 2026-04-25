using Mealie.Domain.Entities.Ingredients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Ingredients;

public class IngredientFoodConfiguration : IEntityTypeConfiguration<IngredientFood>
{
    public void Configure(EntityTypeBuilder<IngredientFood> builder)
    {
        builder.ToTable("ingredient_foods");
        builder.HasKey(f => f.Id);
        builder.HasIndex(f => f.GroupId);
        builder.HasOne(f => f.Group)
               .WithMany(g => g.Foods)
               .HasForeignKey(f => f.GroupId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.Unit)
               .WithMany(u => u.Foods)
               .HasForeignKey(f => f.UnitId)
               .OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(f => f.Aliases)
               .WithOne(a => a.Food)
               .HasForeignKey(a => a.FoodId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
