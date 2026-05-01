using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Application.Dtos.Recipes;
using Microsoft.EntityFrameworkCore;

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

public record GetPendingExportsQuery : IQuery<IList<ExportFileInfo>>
{
    public Task<IList<ExportFileInfo>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var dataDir = services.Settings.Value.DataDir;
        var exportsDir = Path.Combine(dataDir, "exports");
        if (!Directory.Exists(exportsDir)) return Task.FromResult<IList<ExportFileInfo>>([]);
        var files = Directory.GetFiles(exportsDir, "*.zip")
            .Select(f => { var info = new FileInfo(f); return new ExportFileInfo { FileName = info.Name, Size = info.Length, CreatedAt = info.CreationTimeUtc }; })
            .OrderByDescending(f => f.CreatedAt)
            .ToList<ExportFileInfo>();
        return Task.FromResult<IList<ExportFileInfo>>(files);
    }
}

public record ExportRecipeQuery(string Slug) : IQuery<(byte[] Data, string FileName)?>
{
    public async Task<(byte[] Data, string FileName)?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Nutrition)
            .Include(r => r.Settings)
            .FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null) return null;
        var json = JsonSerializer.Serialize(recipe, ExportSerializerOptions.Options);
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            await ExportHelpers.WriteRecipeToArchiveAsync(archive, recipe.Slug, json);
        ms.Seek(0, SeekOrigin.Begin);
        return (ms.ToArray(), $"{recipe.Slug}.zip");
    }
}

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
        if (recipes.Count == 0) return null;
        var exportsDir = Path.Combine(dataDir, "exports");
        Directory.CreateDirectory(exportsDir);
        var fileName = $"mealie-recipes-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
        var filePath = Path.Combine(exportsDir, fileName);
        using var fs = System.IO.File.Create(filePath);
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, true))
        {
            foreach (var recipe in recipes)
            {
                var json = JsonSerializer.Serialize(recipe, ExportSerializerOptions.Options);
                await ExportHelpers.WriteRecipeToArchiveAsync(archive, recipe.Slug, json);
                var imageFile = Path.Combine(dataDir, "recipes", recipe.Id.ToString(), "images", "original.webp");
                if (System.IO.File.Exists(imageFile))
                {
                    var imageEntry = archive.CreateEntry($"{recipe.Slug}/images/original.webp", CompressionLevel.Fastest);
                    await using var imageStream = imageEntry.Open();
                    await using var srcStream = System.IO.File.OpenRead(imageFile);
                    await srcStream.CopyToAsync(imageStream, ct);
                }
            }
        }
        var info = new FileInfo(filePath);
        return new ExportFileInfo { FileName = fileName, Size = info.Length, CreatedAt = info.CreationTimeUtc };
    }
}

public record DownloadExportQuery(string FileName) : IQuery<(Stream Stream, string FileName)?>
{
    public Task<(Stream Stream, string FileName)?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(FileName) || FileName.Contains('/') || FileName.Contains('\\') || FileName.Contains(".."))
            return Task.FromResult<(Stream, string)?>(null);
        var filePath = Path.Combine(services.Settings.Value.DataDir, "exports", FileName);
        if (!System.IO.File.Exists(filePath)) return Task.FromResult<(Stream, string)?>(null);
        Stream stream = System.IO.File.OpenRead(filePath);
        return Task.FromResult<(Stream, string)?>((stream, FileName));
    }
}

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
