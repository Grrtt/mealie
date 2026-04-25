using Mealie.Domain.Entities.Organizers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Organizers;

public class CookbookConfiguration : IEntityTypeConfiguration<Cookbook>
{
    public void Configure(EntityTypeBuilder<Cookbook> builder)
    {
        builder.ToTable("cookbooks");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired();
        builder.HasIndex(c => c.GroupId);
        builder.HasIndex(c => c.HouseholdId);
        builder.HasOne(c => c.Group)
               .WithMany(g => g.Cookbooks)
               .HasForeignKey(c => c.GroupId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.Household)
               .WithMany(h => h.Cookbooks)
               .HasForeignKey(c => c.HouseholdId)
               .OnDelete(DeleteBehavior.Restrict);

        // Many-to-many filter relationships (table names from Python source)
        builder.HasMany(c => c.Categories)
               .WithMany()
               .UsingEntity(j => j.ToTable("cookbooks_to_categories"));
        builder.HasMany(c => c.Tags)
               .WithMany()
               .UsingEntity(j => j.ToTable("cookbooks_to_tags"));
        builder.HasMany(c => c.Tools)
               .WithMany()
               .UsingEntity(j => j.ToTable("cookbooks_to_tools"));
    }
}
