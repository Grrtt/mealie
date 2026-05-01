using Mealie.Domain.Entities.Organizers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mealie.Infrastructure.Data.Configurations.Organizers;

public class GroupInviteTokenConfiguration : IEntityTypeConfiguration<GroupInviteToken>
{
    public void Configure(EntityTypeBuilder<GroupInviteToken> builder)
    {
        // Python table name: invite_tokens
        builder.ToTable("invite_tokens");
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.GroupId);
        builder.HasOne(t => t.Group)
            .WithMany(g => g.InviteTokens)
            .HasForeignKey(t => t.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
