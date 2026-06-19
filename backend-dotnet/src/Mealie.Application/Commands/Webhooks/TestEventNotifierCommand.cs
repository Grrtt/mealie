using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Commands.Webhooks;

public record TestEventNotifierCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var notifier = await services.Db.EventNotifiers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.HouseholdId == HouseholdId && e.Id == Id, ct);
        if (notifier is null)
        {
            return false;
        }

        var logger = services.LoggerFactory.CreateLogger("EventNotifierCommands");
        logger.LogInformation("Test notification sent to {ApprisUrl}", notifier.ApprisUrl);
        return true;
    }
}
