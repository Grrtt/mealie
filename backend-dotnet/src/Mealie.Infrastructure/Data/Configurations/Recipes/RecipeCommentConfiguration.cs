using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Recipes;

public class RecipeCommentConfiguration : IEntityTypeConfiguration<RecipeComment>
{
    public void Configure(EntityTypeBuilder<RecipeComment> builder)
    {
        builder.ToTable("recipe_comments");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.RecipeId);
        builder.HasOne(c => c.Recipe)
               .WithMany(r => r.Comments)
               .HasForeignKey(c => c.RecipeId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.User)
               .WithMany(u => u.Comments)
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
