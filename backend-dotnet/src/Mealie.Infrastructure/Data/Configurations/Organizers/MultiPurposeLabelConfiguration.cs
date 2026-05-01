using Mealie.Domain.Entities.Organizers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Organizers;

public class MultiPurposeLabelConfiguration : IEntityTypeConfiguration<MultiPurposeLabel>
{
    public void Configure(EntityTypeBuilder<MultiPurposeLabel> builder)
    {
        builder.ToTable("multi_purpose_labels");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).IsRequired();
        builder.HasIndex(l => l.GroupId);
        builder.HasOne(l => l.Group)
            .WithMany(g => g.Labels)
            .HasForeignKey(l => l.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
