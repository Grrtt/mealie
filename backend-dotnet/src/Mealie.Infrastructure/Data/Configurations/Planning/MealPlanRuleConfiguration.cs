using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Planning;

public class MealPlanRuleConfiguration : IEntityTypeConfiguration<MealPlanRule>
{
    public void Configure(EntityTypeBuilder<MealPlanRule> builder)
    {
        builder.ToTable("group_meal_plan_rules");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.GroupId);
        builder.HasIndex(r => r.HouseholdId);
        builder.Property(r => r.Day).IsRequired().HasDefaultValue("unset");
        builder.Property(r => r.EntryType).IsRequired().HasDefaultValue("unset");
        builder.Property(r => r.QueryFilterString).IsRequired().HasDefaultValue(string.Empty).HasColumnName("query_filter_string");

        builder.HasOne(r => r.Group)
               .WithMany()
               .HasForeignKey(r => r.GroupId)
               .OnDelete(DeleteBehavior.Cascade);

        // plan_rules_to_tags: columns plan_rule_id / tag_id
        builder.HasMany(r => r.Tags)
               .WithMany()
               .UsingEntity<Dictionary<string, object>>(
                   "plan_rules_to_tags",
                   j => j.HasOne<Tag>().WithMany().HasForeignKey("tag_id").OnDelete(DeleteBehavior.Cascade),
                   j => j.HasOne<MealPlanRule>().WithMany().HasForeignKey("plan_rule_id").OnDelete(DeleteBehavior.Cascade),
                   j => j.HasKey("plan_rule_id", "tag_id"));

        // plan_rules_to_categories: columns group_plan_rule_id / category_id
        builder.HasMany(r => r.Categories)
               .WithMany()
               .UsingEntity<Dictionary<string, object>>(
                   "plan_rules_to_categories",
                   j => j.HasOne<Category>().WithMany().HasForeignKey("category_id").OnDelete(DeleteBehavior.Cascade),
                   j => j.HasOne<MealPlanRule>().WithMany().HasForeignKey("group_plan_rule_id").OnDelete(DeleteBehavior.Cascade),
                   j => j.HasKey("group_plan_rule_id", "category_id"));

        // plan_rules_to_households: columns group_plan_rule_id / household_id
        builder.HasMany(r => r.Households)
               .WithMany()
               .UsingEntity<Dictionary<string, object>>(
                   "plan_rules_to_households",
                   j => j.HasOne<Household>().WithMany().HasForeignKey("household_id").OnDelete(DeleteBehavior.Cascade),
                   j => j.HasOne<MealPlanRule>().WithMany().HasForeignKey("group_plan_rule_id").OnDelete(DeleteBehavior.Cascade),
                   j => j.HasKey("group_plan_rule_id", "household_id"));
    }
}

