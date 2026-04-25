using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Planning;

namespace Mealie.Domain.Entities.Organizers;

public class MultiPurposeLabel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public ICollection<ShoppingListItem> ShoppingItems { get; set; } = [];
}
