using Mealie.Application.Dtos.Recipes;

namespace Mealie.Application.Queries.Recipes;

public record GetPendingExportsQuery : IQuery<IList<ExportFileInfo>>
{
    public Task<IList<ExportFileInfo>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var dataDir = services.Settings.Value.DataDir;
        var exportsDir = Path.Combine(dataDir, "exports");
        if (!Directory.Exists(exportsDir))
        {
            return Task.FromResult<IList<ExportFileInfo>>([]);
        }

        var files = Directory.GetFiles(exportsDir, "*.zip")
            .Select(f =>
            {
                var info = new FileInfo(f);
                return new ExportFileInfo
                    { FileName = info.Name, Size = info.Length, CreatedAt = info.CreationTimeUtc };
            })
            .OrderByDescending(f => f.CreatedAt)
            .ToList<ExportFileInfo>();
        return Task.FromResult<IList<ExportFileInfo>>(files);
    }
}
