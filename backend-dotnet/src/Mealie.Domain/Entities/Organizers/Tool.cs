using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Recipes;

namespace Mealie.Domain.Entities.Organizers;

public class Tool
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public bool OnHand { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public ICollection<Recipe> Recipes { get; set; } = [];
}
