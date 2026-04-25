using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Settings;

public class GroupPreferencesConfiguration : IEntityTypeConfiguration<GroupPreferences>
{
    public void Configure(EntityTypeBuilder<GroupPreferences> builder)
    {
        builder.ToTable("group_preferences");
        builder.HasKey(p => p.Id);
        builder.HasOne(p => p.Group)
               .WithOne(g => g.Preferences)
               .HasForeignKey<GroupPreferences>(p => p.GroupId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
