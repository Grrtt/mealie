using Mealie.Domain.Entities.Organizers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Organizers;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired();
        builder.HasIndex(t => t.GroupId);
        builder.HasOne(t => t.Group)
               .WithMany(g => g.Tags)
               .HasForeignKey(t => t.GroupId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
