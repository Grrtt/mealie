using Mealie.Application.Common;
using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Households;

public class HouseholdService(ApplicationDbContext db, ILogger<HouseholdService> logger) : IHouseholdService
{
    public async Task<HouseholdResponse?> GetHouseholdAsync(Guid householdId, CancellationToken ct = default)
    {
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == householdId, ct);
        if (h is null) return null;
        return MapToResponse(h);
    }

    public async Task<IList<HouseholdResponse>> GetHouseholdsForGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        return await db.Households.IgnoreQueryFilters()
            .Where(h => h.GroupId == groupId)
            .Select(h => new HouseholdResponse { Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId })
            .ToListAsync(ct);
    }

    public async Task<HouseholdResponse?> UpdateHouseholdAsync(Guid householdId, UpdateHouseholdRequest request, CancellationToken ct = default)
    {
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == householdId, ct);
        if (h is null) return null;
        h.Name = request.Name;
        h.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(h);
    }

    public async Task<HouseholdResponse> CreateHouseholdAsync(Guid groupId, CreateHouseholdRequest request, CancellationToken ct = default)
    {
        var slug = SlugHelper.Generate(request.Name);
        var household = new Household
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = slug,
            GroupId = groupId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Households.Add(household);
        await db.SaveChangesAsync(ct);
        return MapToResponse(household);
    }

    public async Task<bool> DeleteHouseholdAsync(Guid householdId, CancellationToken ct = default)
    {
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == householdId, ct);
        if (h is null) return false;
        db.Households.Remove(h);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IList<UserSummaryDto>> GetMembersAsync(Guid householdId, CancellationToken ct = default)
    {
        return await db.Users.IgnoreQueryFilters()
            .Where(u => u.HouseholdId == householdId)
            .Select(u => new UserSummaryDto { Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email })
            .ToListAsync(ct);
    }

    public async Task<HouseholdStatisticsResponse> GetStatisticsAsync(Guid householdId, CancellationToken ct = default)
    {
        var totalRecipes = await db.Recipes.IgnoreQueryFilters().CountAsync(r => r.HouseholdId == householdId, ct);
        var totalUsers = await db.Users.IgnoreQueryFilters().CountAsync(u => u.HouseholdId == householdId, ct);

        var household = await db.Households.IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == householdId, ct);

        int totalCategories = 0, totalTags = 0, totalTools = 0;
        if (household is not null)
        {
            totalCategories = await db.Categories.IgnoreQueryFilters().CountAsync(c => c.GroupId == household.GroupId, ct);
            totalTags = await db.Tags.IgnoreQueryFilters().CountAsync(t => t.GroupId == household.GroupId, ct);
            totalTools = await db.Tools.IgnoreQueryFilters().CountAsync(t => t.GroupId == household.GroupId, ct);
        }

        return new HouseholdStatisticsResponse
        {
            TotalRecipes = totalRecipes,
            TotalUsers = totalUsers,
            TotalCategories = totalCategories,
            TotalTags = totalTags,
            TotalTools = totalTools
        };
    }

    private static HouseholdResponse MapToResponse(Household h) => new()
    {
        Id = h.Id,
        Name = h.Name,
        Slug = h.Slug,
        GroupId = h.GroupId
    };
}
