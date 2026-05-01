using Mealie.Domain.Entities.Ingredients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Ingredients;

public class IngredientUnitAliasConfiguration : IEntityTypeConfiguration<IngredientUnitAlias>
{
    public void Configure(EntityTypeBuilder<IngredientUnitAlias> builder)
    {
        builder.ToTable("ingredient_units_aliases");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.UnitId);
        builder.HasOne(a => a.Unit)
            .WithMany(u => u.Aliases)
            .HasForeignKey(a => a.UnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
