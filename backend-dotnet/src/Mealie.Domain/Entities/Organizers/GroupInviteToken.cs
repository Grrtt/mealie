using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Organizers;

public class GroupInviteToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public Guid? HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
}
