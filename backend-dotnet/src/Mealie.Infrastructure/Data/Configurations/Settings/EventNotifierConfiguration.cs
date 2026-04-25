using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Settings;

public class EventNotifierConfiguration : IEntityTypeConfiguration<EventNotifier>
{
    public void Configure(EntityTypeBuilder<EventNotifier> builder)
    {
        // Python table name: group_events_notifiers
        builder.ToTable("group_events_notifiers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired();
        builder.HasIndex(e => e.GroupId);
        builder.HasIndex(e => e.HouseholdId);
        builder.HasOne(e => e.Group)
               .WithMany()
               .HasForeignKey(e => e.GroupId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Household)
               .WithMany(h => h.EventNotifiers)
               .HasForeignKey(e => e.HouseholdId)
               .OnDelete(DeleteBehavior.Restrict);

        // Options stored in a separate table to match Python source schema
        builder.OwnsOne(e => e.Options, o =>
        {
            o.ToTable("group_events_notifier_options");
        });
    }
}
