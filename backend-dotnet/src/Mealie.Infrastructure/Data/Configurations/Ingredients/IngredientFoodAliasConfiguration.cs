using Mealie.Domain.Entities.Ingredients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Ingredients;

public class IngredientFoodAliasConfiguration : IEntityTypeConfiguration<IngredientFoodAlias>
{
    public void Configure(EntityTypeBuilder<IngredientFoodAlias> builder)
    {
        // Python table name: ingredient_foods_aliases
        builder.ToTable("ingredient_foods_aliases");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.FoodId);
    }
}
