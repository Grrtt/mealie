using Mealie.Application.Dtos.Ingredients;
using Mealie.Application.Services.Ingredients;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Ingredients;

[ApiController]
[Route("api/foods")]
public class FoodsController(IFoodService foodService, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<FoodResponse>>> GetFoods(
        [FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        return Ok(await foodService.GetFoodsAsync(CurrentGroupId, pagination, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FoodResponse>> GetFood(Guid id, CancellationToken ct)
    {
        var food = await foodService.GetByIdAsync(CurrentGroupId, id, ct);
        if (food is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(food);
    }

    [HttpPost]
    public async Task<ActionResult<FoodResponse>> CreateFood([FromBody] CreateFoodRequest request, CancellationToken ct)
    {
        var food = await foodService.CreateAsync(CurrentGroupId, request, ct);
        return CreatedAtAction(nameof(GetFood), new { id = food.Id }, food);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FoodResponse>> UpdateFood(Guid id, [FromBody] UpdateFoodRequest request,
        CancellationToken ct)
    {
        var food = await foodService.UpdateAsync(CurrentGroupId, id, request, ct);
        if (food is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(food);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<FoodResponse>> PatchFood(Guid id, [FromBody] UpdateFoodRequest request,
        CancellationToken ct)
    {
        return await UpdateFood(id, request, ct);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFood(Guid id, CancellationToken ct)
    {
        var deleted = await foodService.DeleteAsync(CurrentGroupId, id, ct);
        if (!deleted)
        {
            return NotFoundOrForbidden();
        }

        return NoContent();
    }

    [HttpPut("merge")]
    public async Task<IActionResult> MergeFoods([FromBody] MergeFoodRequest request, CancellationToken ct)
    {
        var success = await foodService.MergeAsync(CurrentGroupId, request.FromFood, request.ToFood, ct);
        if (!success)
        {
            return NotFoundOrForbidden();
        }

        return Ok(new { detail = "Foods merged successfully" });
    }
}

public class MergeFoodRequest
{
    public Guid FromFood { get; set; }
    public Guid ToFood { get; set; }
}
