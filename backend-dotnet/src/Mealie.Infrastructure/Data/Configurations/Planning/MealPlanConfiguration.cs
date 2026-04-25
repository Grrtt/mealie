using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Planning;

public class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.ToTable("group_meal_plans");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.Date);
        builder.HasIndex(m => m.GroupId);
        builder.HasIndex(m => m.HouseholdId);
        builder.HasOne(m => m.Group)
               .WithMany(g => g.MealPlans)
               .HasForeignKey(m => m.GroupId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.Household)
               .WithMany(h => h.MealPlans)
               .HasForeignKey(m => m.HouseholdId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Recipe)
               .WithMany()
               .HasForeignKey(m => m.RecipeId)
               .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
