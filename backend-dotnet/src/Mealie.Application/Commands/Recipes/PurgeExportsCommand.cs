using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record PurgeExportsCommand : IQuery<int>
{
    public Task<int> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var exportsDir = Path.Combine(services.Settings.Value.DataDir, "exports");
        if (!Directory.Exists(exportsDir)) return Task.FromResult(0);
        var deleted = 0;
        foreach (var file in Directory.GetFiles(exportsDir))
        {
            try { System.IO.File.Delete(file); deleted++; }
            catch { /* ignore */ }
        }
        return Task.FromResult(deleted);
    }
}