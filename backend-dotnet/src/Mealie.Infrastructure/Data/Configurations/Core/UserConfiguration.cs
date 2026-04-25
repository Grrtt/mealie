using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Core;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.FullName);
        builder.HasIndex(u => u.GroupId);
        builder.HasIndex(u => u.HouseholdId);
        builder.Property(u => u.AuthMethod).HasConversion<string>();
        builder.HasOne(u => u.Group)
               .WithMany(g => g.Users)
               .HasForeignKey(u => u.GroupId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(u => u.Household)
               .WithMany(h => h.Users)
               .HasForeignKey(u => u.HouseholdId)
               .OnDelete(DeleteBehavior.SetNull);
        // FavoriteRecipes many-to-many — table name from Python source
        builder.HasMany(u => u.FavoriteRecipes)
               .WithMany()
               .UsingEntity(j => j.ToTable("users_to_recipes"));
    }
}
