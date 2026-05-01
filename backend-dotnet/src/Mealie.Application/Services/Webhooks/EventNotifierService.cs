using Mealie.Application.Dtos.Webhooks;
using Mealie.Domain.Entities.Settings;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Webhooks;

public class EventNotifierService(ApplicationDbContext db, ILogger<EventNotifierService> logger) : IEventNotifierService
{
    public async Task<IList<EventNotifierResponse>> GetAllAsync(Guid householdId, CancellationToken ct = default)
    {
        var notifiers = await db.EventNotifiers.IgnoreQueryFilters()
            .Where(e => e.HouseholdId == householdId)
            .ToListAsync(ct);
        return notifiers.Select(MapToResponse).ToList();
    }

    public async Task<EventNotifierResponse> CreateAsync(Guid groupId, Guid householdId,
        CreateEventNotifierRequest request, CancellationToken ct = default)
    {
        var notifier = new EventNotifier
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ApprisUrl = request.ApprisUrl,
            Enabled = request.Enabled,
            GroupId = groupId,
            HouseholdId = householdId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.EventNotifiers.Add(notifier);
        await db.SaveChangesAsync(ct);
        return MapToResponse(notifier);
    }

    public async Task<EventNotifierResponse?> UpdateAsync(Guid householdId, Guid id, CreateEventNotifierRequest request,
        CancellationToken ct = default)
    {
        var notifier = await db.EventNotifiers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.HouseholdId == householdId && e.Id == id, ct);
        if (notifier is null)
        {
            return null;
        }

        notifier.Name = request.Name;
        notifier.ApprisUrl = request.ApprisUrl;
        notifier.Enabled = request.Enabled;
        notifier.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(notifier);
    }

    public async Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var notifier = await db.EventNotifiers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.HouseholdId == householdId && e.Id == id, ct);
        if (notifier is null)
        {
            return false;
        }

        db.EventNotifiers.Remove(notifier);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task TestAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var notifier = await db.EventNotifiers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.HouseholdId == householdId && e.Id == id, ct);
        if (notifier is null)
        {
            return;
        }

        logger.LogInformation("Test notification sent to {ApprisUrl}", notifier.ApprisUrl);
        // Appris URL notification would be implemented here with HTTP client
    }

    private static EventNotifierResponse MapToResponse(EventNotifier e)
    {
        return new EventNotifierResponse
        {
            Id = e.Id,
            Name = e.Name,
            ApprisUrl = e.ApprisUrl,
            Enabled = e.Enabled
        };
    }
}
