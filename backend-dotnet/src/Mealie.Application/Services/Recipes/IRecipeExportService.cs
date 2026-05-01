using Mealie.Application.Dtos.Recipes;

namespace Mealie.Application.Services.Recipes;

public interface IRecipeExportService
{
    Task<IList<ExportFileInfo>> GetExportsAsync(CancellationToken ct = default);
    Task<IList<ExportFileInfo>> GetPendingExportsAsync(CancellationToken ct = default);
    Task<(byte[] Data, string FileName)?> ExportRecipeAsync(string slug, CancellationToken ct = default);
    Task<ExportFileInfo?> BulkExportAsync(IList<string> slugs, Guid groupId, CancellationToken ct = default);
    Task<(Stream Stream, string FileName)?> DownloadExportAsync(string fileName, CancellationToken ct = default);
    Task<int> PurgeExportsAsync(CancellationToken ct = default);
}
