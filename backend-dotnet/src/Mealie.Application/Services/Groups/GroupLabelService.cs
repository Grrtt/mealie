using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Groups;

public interface IGroupLabelService
{
    Task<PaginatedResponse<GroupLabelResponse>> GetLabelsAsync(Guid groupId, PaginationParams pagination, CancellationToken ct = default);
    Task<GroupLabelResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default);
    Task<GroupLabelResponse> CreateAsync(Guid groupId, CreateGroupLabelRequest request, CancellationToken ct = default);
    Task<GroupLabelResponse?> UpdateAsync(Guid groupId, Guid id, UpdateGroupLabelRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default);
}

public class GroupLabelService(ApplicationDbContext db) : IGroupLabelService
{
    public async Task<PaginatedResponse<GroupLabelResponse>> GetLabelsAsync(Guid groupId, PaginationParams pagination, CancellationToken ct = default)
    {
        var query = db.Labels.IgnoreQueryFilters().Where(l => l.GroupId == groupId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(l => l.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(l => MapToResponse(l))
            .ToListAsync(ct);
        return new PaginatedResponse<GroupLabelResponse>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items
        };
    }

    public async Task<GroupLabelResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var label = await db.Labels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.GroupId == groupId && l.Id == id, ct);
        if (label is null) return null;
        return MapToResponse(label);
    }

    public async Task<GroupLabelResponse> CreateAsync(Guid groupId, CreateGroupLabelRequest request, CancellationToken ct = default)
    {
        var label = new MultiPurposeLabel
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Color = request.Color,
            GroupId = groupId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
        };
        db.Labels.Add(label);
        await db.SaveChangesAsync(ct);
        return MapToResponse(label);
    }

    public async Task<GroupLabelResponse?> UpdateAsync(Guid groupId, Guid id, UpdateGroupLabelRequest request, CancellationToken ct = default)
    {
        var label = await db.Labels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.GroupId == groupId && l.Id == id, ct);
        if (label is null) return null;

        if (request.Name is not null) label.Name = request.Name;
        if (request.Color is not null) label.Color = request.Color;
        label.UpdateAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return MapToResponse(label);
    }

    public async Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var label = await db.Labels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.GroupId == groupId && l.Id == id, ct);
        if (label is null) return false;

        db.Labels.Remove(label);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static GroupLabelResponse MapToResponse(MultiPurposeLabel l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        Color = l.Color,
        GroupId = l.GroupId,
        CreatedAt = l.CreatedAt,
        UpdateAt = l.UpdateAt,
    };
}
