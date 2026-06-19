namespace Mealie.Application.Dtos.RecipeActions;

public class RecipeActionResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ActionType { get; set; } = "link";
}

public class CreateRecipeActionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ActionType { get; set; } = "link";
}

public class UpdateRecipeActionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ActionType { get; set; } = "link";
}

public class RecipeActionTriggerRequest
{
    public int? RecipeScale { get; set; }
}

public class RecipeActionTriggerResponse
{
    public string Url { get; set; } = string.Empty;
    public bool Triggered { get; set; }
    public int StatusCode { get; set; }
}
