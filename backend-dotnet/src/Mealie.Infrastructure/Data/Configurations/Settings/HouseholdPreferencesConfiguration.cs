using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Settings;

public class HouseholdPreferencesConfiguration : IEntityTypeConfiguration<HouseholdPreferences>
{
    public void Configure(EntityTypeBuilder<HouseholdPreferences> builder)
    {
        builder.ToTable("household_preferences");
        builder.HasKey(p => p.Id);
        builder.HasOne(p => p.Household)
            .WithOne(h => h.Preferences)
            .HasForeignKey<HouseholdPreferences>(p => p.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
