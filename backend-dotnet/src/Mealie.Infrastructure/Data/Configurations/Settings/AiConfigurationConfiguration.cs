using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Settings;

public class AiConfigurationConfiguration : IEntityTypeConfiguration<AiConfiguration>
{
    public void Configure(EntityTypeBuilder<AiConfiguration> builder)
    {
        builder.ToTable("ai_configurations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired();
        builder.Property(e => e.ProviderType).IsRequired();
        builder.Property(e => e.EncryptedApiKey);
        builder.Property(e => e.BaseUrl);
        builder.Property(e => e.DefaultModel);
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.EnableImageServices).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.EnableTranscriptionServices).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.IsActive).HasDatabaseName("ix_ai_configurations_is_active");
    }
}
