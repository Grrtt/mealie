using Mealie.Application.Dtos.Recipes;

namespace Mealie.Application.Queries.Recipes;

public record GetExportsQuery : IQuery<IList<ExportFileInfo>>
{
    public Task<IList<ExportFileInfo>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var dataDir = services.Settings.Value.DataDir;
        var exportsDir = Path.Combine(dataDir, "backups", "recipes");
        if (!Directory.Exists(exportsDir)) return Task.FromResult<IList<ExportFileInfo>>([]);
        var files = Directory.GetFiles(exportsDir, "*.json")
            .Concat(Directory.GetFiles(exportsDir, "*.zip"))
            .Select(f => { var info = new FileInfo(f); return new ExportFileInfo { FileName = info.Name, Size = info.Length, CreatedAt = info.CreationTimeUtc }; })
            .OrderByDescending(f => f.CreatedAt)
            .ToList();
        return Task.FromResult<IList<ExportFileInfo>>(files);
    }
}
