using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Recipes;

public class RecipeNoteConfiguration : IEntityTypeConfiguration<RecipeNote>
{
    public void Configure(EntityTypeBuilder<RecipeNote> builder)
    {
        builder.ToTable("notes");
        builder.HasKey(n => n.Id);
        builder.HasOne(n => n.Recipe)
            .WithMany(r => r.Notes)
            .HasForeignKey(n => n.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
