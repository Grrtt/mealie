using Mealie.Application.Queries;

namespace Mealie.Application.Commands.Recipes;

public record PurgeExportsCommand : IQuery<int>
{
    public Task<int> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var exportsDir = Path.Combine(services.Settings.Value.DataDir, "exports");
        if (!Directory.Exists(exportsDir))
        {
            return Task.FromResult(0);
        }

        var deleted = 0;
        foreach (var file in Directory.GetFiles(exportsDir))
        {
            try
            {
                File.Delete(file);
                deleted++;
            }
            catch
            {
                /* ignore */
            }
        }

        return Task.FromResult(deleted);
    }
}
