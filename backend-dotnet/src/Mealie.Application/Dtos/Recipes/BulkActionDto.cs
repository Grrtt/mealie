namespace Mealie.Application.Dtos.Recipes;

public class BulkActionRequest
{
    public IList<string> Recipes { get; set; } = [];
}

public class BulkTagRequest
{
    public IList<string> Recipes { get; set; } = [];
    public IList<string> Tags { get; set; } = [];
}

public class BulkCategorizeRequest
{
    public IList<string> Recipes { get; set; } = [];
    public IList<string> Categories { get; set; } = [];
}
