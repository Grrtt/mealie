using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Recipes;

public class RecipeAssetConfiguration : IEntityTypeConfiguration<RecipeAsset>
{
    public void Configure(EntityTypeBuilder<RecipeAsset> builder)
    {
        builder.ToTable("recipe_assets");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.RecipeId);
        builder.HasOne(a => a.Recipe)
            .WithMany(r => r.Assets)
            .HasForeignKey(a => a.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
