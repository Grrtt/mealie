using Mealie.Application.Dtos.Recipes;

namespace Mealie.Application.Services.Recipes;

public interface IRecipeExportService
{
    Task<IList<ExportFileInfo>> GetExportsAsync(CancellationToken ct = default);
    Task<(byte[] Data, string FileName)?> ExportRecipeAsync(string slug, CancellationToken ct = default);
}
