using Mealie.Application.Queries;

namespace Mealie.Application.Commands.Webhooks;

public record TestEventNotifierCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        await services.NotifierService.TestAsync(HouseholdId, Id, ct);
        return true;
    }
}
