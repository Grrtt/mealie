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
    private string ExportsDir => Path.Combine(_dataDir, "exports");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

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

    public Task<IList<ExportFileInfo>> GetPendingExportsAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(ExportsDir))
        {
            return Task.FromResult<IList<ExportFileInfo>>([]);
        }

        var files = Directory.GetFiles(ExportsDir, "*.zip")
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
            .ToList<ExportFileInfo>();

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

        var json = JsonSerializer.Serialize(recipe, SerializerOptions);

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            await WriteRecipeToArchiveAsync(archive, recipe.Slug, json);
        }

        ms.Seek(0, SeekOrigin.Begin);
        return (ms.ToArray(), $"{recipe.Slug}.zip");
    }

    /// <summary>
    /// Creates a combined ZIP containing all specified recipes (JSON + images) and saves it to the exports directory.
    /// Returns file info for the saved ZIP.
    /// </summary>
    public async Task<ExportFileInfo?> BulkExportAsync(IList<string> slugs, Guid groupId, CancellationToken ct = default)
    {
        var recipes = await db.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Nutrition)
            .Include(r => r.Settings)
            .Where(r => r.GroupId == groupId && slugs.Contains(r.Slug))
            .ToListAsync(ct);

        if (recipes.Count == 0)
        {
            return null;
        }

        Directory.CreateDirectory(ExportsDir);

        var fileName = $"mealie-recipes-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
        var filePath = Path.Combine(ExportsDir, fileName);

        using var fs = System.IO.File.Create(filePath);
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, true))
        {
            foreach (var recipe in recipes)
            {
                var json = JsonSerializer.Serialize(recipe, SerializerOptions);
                await WriteRecipeToArchiveAsync(archive, recipe.Slug, json);

                // Include local image if present
                var imageDir = Path.Combine(_dataDir, "recipes", recipe.Id.ToString(), "images");
                var imageFile = Path.Combine(imageDir, "original.webp");
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
        return new ExportFileInfo
        {
            FileName = fileName,
            Size = info.Length,
            CreatedAt = info.CreationTimeUtc
        };
    }

    public Task<(Stream Stream, string FileName)?> DownloadExportAsync(string fileName, CancellationToken ct = default)
    {
        // Sanitize: only allow simple file names with no path traversal
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
        {
            return Task.FromResult<(Stream, string)?>(null);
        }

        var filePath = Path.Combine(ExportsDir, fileName);
        if (!System.IO.File.Exists(filePath))
        {
            return Task.FromResult<(Stream, string)?>(null);
        }

        Stream stream = System.IO.File.OpenRead(filePath);
        return Task.FromResult<(Stream, string)?>((stream, fileName));
    }

    public Task<int> PurgeExportsAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(ExportsDir))
        {
            return Task.FromResult(0);
        }

        var deleted = 0;
        foreach (var file in Directory.GetFiles(ExportsDir))
        {
            try
            {
                System.IO.File.Delete(file);
                deleted++;
            }
            catch
            {
                // ignore errors on individual files
            }
        }

        return Task.FromResult(deleted);
    }

    private static async Task WriteRecipeToArchiveAsync(ZipArchive archive, string slug, string json)
    {
        var entry = archive.CreateEntry($"{slug}/{slug}.json", CompressionLevel.Fastest);
        await using var entryStream = entry.Open();
        await using var writer = new StreamWriter(entryStream);
        await writer.WriteAsync(json);
    }
}
