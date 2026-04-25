using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Organizers;

public class Cookbook
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public bool Public { get; set; }
    public bool RequireAllCategories { get; set; }
    public int Position { get; set; }
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public Household Household { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<Tool> Tools { get; set; } = [];
}
