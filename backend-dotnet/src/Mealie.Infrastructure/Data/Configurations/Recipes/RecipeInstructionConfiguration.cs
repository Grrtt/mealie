using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Recipes;

public class RecipeInstructionConfiguration : IEntityTypeConfiguration<RecipeInstruction>
{
    public void Configure(EntityTypeBuilder<RecipeInstruction> builder)
    {
        builder.ToTable("recipe_instructions");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Text).IsRequired();
        builder.HasIndex(i => i.RecipeId);
        builder.HasOne(i => i.Recipe)
               .WithMany(r => r.RecipeInstructions)
               .HasForeignKey(i => i.RecipeId)
               .OnDelete(DeleteBehavior.Cascade);

        // IngredientReferences stored as JSON in the Python source via a junction table;
        // we ignore the navigation and use a raw JSON column for the C# layer.
        builder.Ignore(i => i.IngredientReferences);
        builder.Property<string?>("IngredientReferencesJson")
               .HasColumnName("ingredient_references");
    }
}
