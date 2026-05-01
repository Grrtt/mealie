using Mealie.Domain.Entities.Ingredients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Ingredients;

public class IngredientUnitConfiguration : IEntityTypeConfiguration<IngredientUnit>
{
    public void Configure(EntityTypeBuilder<IngredientUnit> builder)
    {
        builder.ToTable("ingredient_units");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.GroupId);
        builder.HasOne(u => u.Group)
            .WithMany(g => g.Units)
            .HasForeignKey(u => u.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
