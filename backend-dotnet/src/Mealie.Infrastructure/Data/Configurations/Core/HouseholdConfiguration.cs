using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Core;

public class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ToTable("households");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Name).IsRequired().HasMaxLength(255);
        builder.HasIndex(h => h.GroupId);
        builder.HasOne(h => h.Group)
            .WithMany(g => g.Households)
            .HasForeignKey(h => h.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
