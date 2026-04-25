using Mealie.Domain.Entities.Organizers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Organizers;

public class ToolConfiguration : IEntityTypeConfiguration<Tool>
{
    public void Configure(EntityTypeBuilder<Tool> builder)
    {
        builder.ToTable("tools");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired();
        builder.HasIndex(t => t.GroupId);
        builder.HasOne(t => t.Group)
               .WithMany(g => g.Tools)
               .HasForeignKey(t => t.GroupId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
