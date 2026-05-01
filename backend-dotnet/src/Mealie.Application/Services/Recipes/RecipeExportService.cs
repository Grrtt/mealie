using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Application.Dtos.Recipes;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Services.Recipes;

public class RecipeExportService(ApplicationDbContext db, IOptions<AppSettings> settings) : IRecipeExportService
{
    private readonly string _dataDir = settings.Value.DataDir;

    public Task<IList<ExportFileInfo>> GetExportsAsync(CancellationToken ct = default)
    {
        var exportsDir = Path.Combine(_dataDir, "backups", "recipes");
        if (!Directory.Exists(exportsDir))
        {
            return Task.FromResult<IList<ExportFileInfo>>([]);
        }

        var files = Directory.GetFiles(exportsDir, "*.json")
            .Concat(Directory.GetFiles(exportsDir, "*.zip"))
            .Select(f =>
            {
                var info = new FileInfo(f);
                return new ExportFileInfo
                {
                    FileName = info.Name,
                    Size = info.Length,
                    CreatedAt = info.CreationTimeUtc
                };
            })
            .OrderByDescending(f => f.CreatedAt)
            .ToList();

        return Task.FromResult<IList<ExportFileInfo>>(files);
    }

    public async Task<(byte[] Data, string FileName)?> ExportRecipeAsync(string slug, CancellationToken ct = default)
    {
        var recipe = await db.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Nutrition)
            .Include(r => r.Settings)
            .FirstOrDefaultAsync(r => r.Slug == slug, ct);

        if (recipe is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(recipe, new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        });

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry($"{recipe.Slug}.json", CompressionLevel.Fastest);
            await using var entryStream = entry.Open();
            await using var writer = new StreamWriter(entryStream);
            await writer.WriteAsync(json);
        }

        ms.Seek(0, SeekOrigin.Begin);
        return (ms.ToArray(), $"{recipe.Slug}.zip");
    }
}
