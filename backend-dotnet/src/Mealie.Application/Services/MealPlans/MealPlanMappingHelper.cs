using Mealie.Application.Dtos.MealPlans;
using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.MealPlans;

public static class MealPlanMappingHelper
{
    public static IQueryable<MealPlan> WithRecipe(IQueryable<MealPlan> query)
    {
        return query.Include(m => m.Recipe).ThenInclude(r => r!.Tags)
            .Include(m => m.Recipe).ThenInclude(r => r!.Categories);
    }

    public static async Task LoadRecipeNavAsync(ApplicationDbContext db, MealPlan plan, CancellationToken ct = default)
    {
        if (plan.RecipeId is null)
        {
            return;
        }

        await db.Entry(plan).Reference(p => p.Recipe).LoadAsync(ct);
        if (plan.Recipe is not null)
        {
            await db.Entry(plan.Recipe).Collection(r => r.Tags).LoadAsync(ct);
            await db.Entry(plan.Recipe).Collection(r => r.Categories).LoadAsync(ct);
        }
    }

    public static MealPlanResponse MapToResponse(MealPlan plan)
    {
        return new MealPlanResponse
        {
            Id = plan.Id,
            Title = plan.Title,
            Text = plan.Text,
            EntryType = plan.EntryType,
            Date = plan.Date,
            RecipeId = plan.RecipeId,
            Recipe = plan.Recipe is null
                ? null
                : new MealPlanRecipeSummary
                {
                    Id = plan.Recipe.Id.ToString(),
                    Name = plan.Recipe.Name,
                    Slug = plan.Recipe.Slug,
                    Image = plan.Recipe.Image,
                    Description = plan.Recipe.Description,
                    Tags = plan.Recipe.Tags.Select(t => new MealPlanRecipeTagSummary
                        { Id = t.Id.ToString(), GroupId = t.GroupId.ToString(), Name = t.Name, Slug = t.Slug }).ToList(),
                    RecipeCategory = plan.Recipe.Categories.Select(c => new MealPlanRecipeTagSummary
                        { Id = c.Id.ToString(), GroupId = c.GroupId.ToString(), Name = c.Name, Slug = c.Slug }).ToList()
                },
            GroupId = plan.GroupId,
            HouseholdId = plan.HouseholdId,
            UserId = plan.UserId,
            CreatedAt = plan.CreatedAt,
            UpdateAt = plan.UpdateAt
        };
    }
}
