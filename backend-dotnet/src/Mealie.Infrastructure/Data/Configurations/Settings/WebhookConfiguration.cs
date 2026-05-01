using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Settings;

public class WebhookConfiguration : IEntityTypeConfiguration<Webhook>
{
    public void Configure(EntityTypeBuilder<Webhook> builder)
    {
        // Python table name: webhook_urls
        builder.ToTable("webhook_urls");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired();
        builder.Property(w => w.Url).IsRequired();
        builder.HasIndex(w => w.GroupId);
        builder.HasIndex(w => w.HouseholdId);
        builder.HasOne(w => w.Group)
            .WithMany(g => g.Webhooks)
            .HasForeignKey(w => w.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(w => w.Household)
            .WithMany(h => h.Webhooks)
            .HasForeignKey(w => w.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
