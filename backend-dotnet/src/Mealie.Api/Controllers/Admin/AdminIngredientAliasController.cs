using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/ingredient-aliases")]
[Authorize(Roles = "admin")]
public class AdminIngredientAliasController(
    ApplicationDbContext db,
    ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    /// <summary>Returns unresolved ingredient strings (no food match), grouped and counted.</summary>
    [HttpGet("unresolved")]
    public async Task<ActionResult<PaginatedResponse<UnresolvedIngredientResponse>>> GetUnresolved(
        [FromQuery] PaginationParams pagination,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var query = db.RecipeIngredients.IgnoreQueryFilters()
            .Where(i => i.FoodId == null && i.OriginalText != null && i.OriginalText != "")
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(i => i.OriginalText!.Contains(search));
        }

        var grouped = query
            .GroupBy(i => i.OriginalText!)
            .Select(g => new UnresolvedIngredientResponse { RawText = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count);

        var total = await grouped.CountAsync(ct);
        var items = await grouped
            .Skip(pagination.Skip)
            .Take(pagination.PerPage)
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<UnresolvedIngredientResponse>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items
        });
    }

    /// <summary>Returns all existing ingredient food aliases.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<IngredientAliasResponse>>> GetAliases(
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        var query = db.FoodAliases.IgnoreQueryFilters()
            .Include(a => a.Food)
            .Select(a => new IngredientAliasResponse
            {
                Id = a.Id,
                Name = a.Name,
                FoodId = a.FoodId,
                FoodName = a.Food.Name
            });

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PerPage)
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<IngredientAliasResponse>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items
        });
    }

    /// <summary>Creates an ingredient food alias, optionally backfilling existing recipe ingredients.</summary>
    [HttpPost]
    public async Task<ActionResult<IngredientAliasResponse>> CreateAlias(
        [FromBody] CreateIngredientAliasRequest request,
        CancellationToken ct)
    {
        var foodExists = await db.Foods.IgnoreQueryFilters()
            .AnyAsync(f => f.Id == request.FoodId, ct);
        if (!foodExists)
        {
            return NotFound("Food not found");
        }

        var duplicate = await db.FoodAliases.IgnoreQueryFilters()
            .AnyAsync(a => a.Name == request.RawText, ct);
        if (duplicate)
        {
            return Conflict("Alias already exists");
        }

        var alias = new IngredientFoodAlias
        {
            Id = Guid.NewGuid(),
            Name = request.RawText,
            FoodId = request.FoodId
        };
        db.FoodAliases.Add(alias);
        await db.SaveChangesAsync(ct);

        if (request.BackfillRecipes)
        {
            await db.RecipeIngredients.IgnoreQueryFilters()
                .Where(i => i.OriginalText == request.RawText && i.FoodId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.FoodId, request.FoodId), ct);
        }

        var food = await db.Foods.IgnoreQueryFilters()
            .FirstAsync(f => f.Id == request.FoodId, ct);

        var response = new IngredientAliasResponse
        {
            Id = alias.Id,
            Name = alias.Name,
            FoodId = alias.FoodId,
            FoodName = food.Name
        };

        return CreatedAtAction(nameof(GetAliases), response);
    }

    /// <summary>Deletes an ingredient food alias by id.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAlias(Guid id, CancellationToken ct)
    {
        var alias = await db.FoodAliases.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (alias is null)
        {
            return NotFound();
        }

        db.FoodAliases.Remove(alias);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
