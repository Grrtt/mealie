namespace Mealie.Application.Dtos.Groups;

public class GroupLabelResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class CreateGroupLabelRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public class UpdateGroupLabelRequest
{
    public string? Name { get; set; }
    public string? Color { get; set; }
}
