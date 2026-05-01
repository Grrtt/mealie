using Mealie.Application.Dtos.MealPlans;

namespace Mealie.Application.Services.MealPlans;

public interface IMealPlanService
{
    Task<IList<MealPlanResponse>> GetMealPlansAsync(Guid householdId, DateOnly? startDate = null,
        DateOnly? endDate = null, CancellationToken ct = default);

    Task<MealPlanResponse?> GetByIdAsync(Guid householdId, Guid id, CancellationToken ct = default);

    Task<MealPlanResponse> CreateAsync(Guid groupId, Guid householdId, Guid userId, CreateMealPlanRequest request,
        CancellationToken ct = default);

    Task<MealPlanResponse?> CreateRandomAsync(Guid groupId, Guid householdId, Guid userId,
        CreateRandomMealPlanRequest request, CancellationToken ct = default);

    Task<IList<MealPlanResponse>> FillDayAsync(Guid groupId, Guid householdId, Guid userId, FillDayRequest request,
        CancellationToken ct = default);

    Task<IList<MealPlanResponse>> FillWeekAsync(Guid groupId, Guid householdId, Guid userId, FillWeekRequest request,
        CancellationToken ct = default);

    Task<MealPlanResponse?> UpdateAsync(Guid householdId, Guid id, UpdateMealPlanRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default);
}
