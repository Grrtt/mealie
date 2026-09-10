using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Domain.Entities.Recipes;

namespace Mealie.Application.Services.Recipes;

/// <summary>
///     Streams a single recipe into a ZIP archive (<c>{slug}/{slug}.json</c>) directly onto an
///     output stream. Neither the archive nor the serialized JSON is buffered in full in memory,
///     and only asynchronous writes are issued, so response streams with synchronous IO disabled
///     are supported.
/// </summary>
public static class RecipeZipWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    ///     Writes the archive to <paramref name="output" />. The caller owns the stream; it is
    ///     never closed, and writes are flushed before this method returns.
    /// </summary>
    public static async Task WriteRecipeAsync(Stream output, Recipe recipe, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(recipe);

        ct.ThrowIfCancellationRequested();

        await using var archive = await ZipArchive.CreateAsync(
            output, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null, ct);
        await WriteEntryAsync(archive, recipe, ct);
    }

    private static async Task WriteEntryAsync(ZipArchive archive, Recipe recipe, CancellationToken ct)
    {
        var entry = archive.CreateEntry($"{recipe.Slug}/{recipe.Slug}.json", CompressionLevel.Fastest);
        // The entry stream is left for the archive to finalize: its DisposeAsync falls back to a
        // synchronous dispose, whereas ZipArchive.DisposeAsync closes open entries asynchronously.
        var entryStream = await entry.OpenAsync(ct);
        await JsonSerializer.SerializeAsync(entryStream, recipe, SerializerOptions, ct);
    }
}
