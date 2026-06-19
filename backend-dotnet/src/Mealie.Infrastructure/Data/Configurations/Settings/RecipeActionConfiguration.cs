using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Settings;

public class RecipeActionConfiguration : IEntityTypeConfiguration<RecipeAction>
{
    public void Configure(EntityTypeBuilder<RecipeAction> builder)
    {
        builder.ToTable("recipe_actions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).IsRequired();
        builder.Property(r => r.Url).IsRequired();
        builder.Property(r => r.ActionType).IsRequired().HasMaxLength(16);
        builder.HasIndex(r => r.GroupId);
        builder.HasIndex(r => r.HouseholdId);
        builder.HasOne(r => r.Group)
            .WithMany(g => g.RecipeActions)
            .HasForeignKey(r => r.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Household)
            .WithMany(h => h.RecipeActions)
            .HasForeignKey(r => r.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
