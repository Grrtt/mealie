using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record BulkExportCommand(IList<string> Slugs, Guid GroupId) : IQuery<ExportFileInfo?>
{
    public async Task<ExportFileInfo?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var dataDir = services.Settings.Value.DataDir;
        var recipes = await db.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Nutrition)
            .Include(r => r.Settings)
            .Where(r => r.GroupId == GroupId && Slugs.Contains(r.Slug))
            .ToListAsync(ct);
        if (recipes.Count == 0)
        {
            return null;
        }

        var exportsDir = Path.Combine(dataDir, "exports");
        Directory.CreateDirectory(exportsDir);
        var fileName = $"mealie-recipes-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
        var filePath = Path.Combine(exportsDir, fileName);
        using var fs = File.Create(filePath);
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, true))
        {
            foreach (var recipe in recipes)
            {
                var json = JsonSerializer.Serialize(recipe, ExportSerializerOptions.Options);
                await ExportHelpers.WriteRecipeToArchiveAsync(archive, recipe.Slug, json);
                var imageFile = Path.Combine(dataDir, "recipes", recipe.Id.ToString(), "images", "original.webp");
                if (File.Exists(imageFile))
                {
                    var imageEntry =
                        archive.CreateEntry($"{recipe.Slug}/images/original.webp", CompressionLevel.Fastest);
                    await using var imageStream = imageEntry.Open();
                    await using var srcStream = File.OpenRead(imageFile);
                    await srcStream.CopyToAsync(imageStream, ct);
                }
            }
        }

        var info = new FileInfo(filePath);
        return new ExportFileInfo { FileName = fileName, Size = info.Length, CreatedAt = info.CreationTimeUtc };
    }
}

file static class ExportSerializerOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
}

file static class ExportHelpers
{
    public static async Task WriteRecipeToArchiveAsync(ZipArchive archive, string slug, string json)
    {
        var entry = archive.CreateEntry($"{slug}/{slug}.json", CompressionLevel.Fastest);
        await using var entryStream = entry.Open();
        await using var writer = new StreamWriter(entryStream);
        await writer.WriteAsync(json);
    }
}
