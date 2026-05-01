using Mealie.Application.Dtos.MealPlans;

namespace Mealie.Application.Services.MealPlans;

public interface IMealPlanRuleService
{
    Task<IList<MealPlanRuleResponse>> GetAllAsync(Guid groupId, Guid householdId, CancellationToken ct = default);
    Task<MealPlanRuleResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default);

    Task<MealPlanRuleResponse> CreateAsync(Guid groupId, Guid householdId, CreateMealPlanRuleRequest request,
        CancellationToken ct = default);

    Task<MealPlanRuleResponse?> UpdateAsync(Guid groupId, Guid id, UpdateMealPlanRuleRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default);
}
