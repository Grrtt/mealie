using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Recipes;

public class RecipeShareTokenConfiguration : IEntityTypeConfiguration<RecipeShareToken>
{
    public void Configure(EntityTypeBuilder<RecipeShareToken> builder)
    {
        builder.ToTable("recipe_share_tokens");
        builder.HasKey(t => t.Id);
        builder.HasOne(t => t.Recipe)
            .WithMany(r => r.ShareTokens)
            .HasForeignKey(t => t.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.Group)
            .WithMany()
            .HasForeignKey(t => t.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
