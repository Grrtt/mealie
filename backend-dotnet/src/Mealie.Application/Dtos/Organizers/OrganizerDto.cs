namespace Mealie.Application.Dtos.Organizers;

public class TagResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class CategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class ToolResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public bool OnHand { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class CookbookResponse
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
}

public class CreateOrganizerRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateOrganizerRequest
{
    public string? Name { get; set; }
}

public class CreateToolRequest
{
    public string Name { get; set; } = string.Empty;
    public bool OnHand { get; set; }
}

public class UpdateToolRequest
{
    public string? Name { get; set; }
    public bool? OnHand { get; set; }
}

public class CreateCookbookRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Public { get; set; }
    public bool RequireAllCategories { get; set; }
}

public class UpdateCookbookRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? Public { get; set; }
    public bool? RequireAllCategories { get; set; }
}
