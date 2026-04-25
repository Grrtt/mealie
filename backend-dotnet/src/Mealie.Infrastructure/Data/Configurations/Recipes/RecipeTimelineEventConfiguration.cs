using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Recipes;

public class RecipeTimelineEventConfiguration : IEntityTypeConfiguration<RecipeTimelineEvent>
{
    public void Configure(EntityTypeBuilder<RecipeTimelineEvent> builder)
    {
        builder.ToTable("recipe_timeline_events");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.RecipeId);
        builder.HasOne(e => e.Recipe)
               .WithMany(r => r.TimelineEvents)
               .HasForeignKey(e => e.RecipeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
