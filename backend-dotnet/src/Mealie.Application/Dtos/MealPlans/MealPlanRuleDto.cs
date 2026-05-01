namespace Mealie.Application.Dtos.MealPlans;

public class MealPlanRuleTagSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class MealPlanRuleResponse
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid? HouseholdId { get; set; }
    public string Day { get; set; } = "unset";
    public string EntryType { get; set; } = "unset";
    public string QueryFilterString { get; set; } = string.Empty;
    public IList<MealPlanRuleTagSummary> Tags { get; set; } = [];
    public IList<MealPlanRuleTagSummary> Categories { get; set; } = [];
    public IList<Guid> Households { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class CreateMealPlanRuleRequest
{
    public string Day { get; set; } = "unset";
    public string EntryType { get; set; } = "unset";
    public string QueryFilterString { get; set; } = string.Empty;
    public IList<Guid> TagIds { get; set; } = [];
    public IList<Guid> CategoryIds { get; set; } = [];
    public IList<Guid> HouseholdIds { get; set; } = [];
}

public class UpdateMealPlanRuleRequest
{
    public string? Day { get; set; }
    public string? EntryType { get; set; }
    public string? QueryFilterString { get; set; }
    public IList<Guid>? TagIds { get; set; }
    public IList<Guid>? CategoryIds { get; set; }
    public IList<Guid>? HouseholdIds { get; set; }
}
