using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Application.Services.Recipes;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Recipes;

namespace Mealie.UnitTests.Recipes;

public class RecipeZipWriterTests
{
    private static readonly JsonSerializerOptions FormerSerializerOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    [Fact]
    public async Task WriteRecipeAsync_ProducesValidZipWithExpectedEntryAndJson()
    {
        var recipe = CreateRecipe();
        using var output = new MemoryStream();

        await RecipeZipWriter.WriteRecipeAsync(output, recipe);

        output.Position = 0;
        using var archive = new ZipArchive(output, ZipArchiveMode.Read);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal("chicken-soup/chicken-soup.json", entry.FullName);

        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        var json = await reader.ReadToEndAsync();

        // Preserved serializer behavior: PascalCase property names, indented output.
        Assert.Contains("\n  \"Name\": \"Chicken Soup\"", json);
        Assert.Contains("\"Slug\": \"chicken-soup\"", json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Chicken Soup", root.GetProperty("Name").GetString());
        var ingredients = root.GetProperty("RecipeIngredients");
        Assert.Equal(1, ingredients.GetArrayLength());
        Assert.Equal("1 cup broth", ingredients[0].GetProperty("OriginalText").GetString());
    }

    [Fact]
    public async Task WriteRecipeAsync_SupportsAsyncOnlyNonSeekableOutputStream()
    {
        // Large enough that the JSON serializer flushes multiple chunks to the entry stream.
        var recipe = CreateRecipe(ingredientCount: 300);
        var inner = new MemoryStream();
        await using var output = new AsyncOnlyNonSeekableStream(inner);

        await RecipeZipWriter.WriteRecipeAsync(output, recipe);

        inner.Position = 0;
        using var archive = new ZipArchive(inner, ZipArchiveMode.Read);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal("chicken-soup/chicken-soup.json", entry.FullName);

        using var entryStream = entry.Open();
        using var doc = await JsonDocument.ParseAsync(entryStream);
        Assert.Equal("Chicken Soup", doc.RootElement.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task WriteRecipeAsync_WritesProgressivelyToOutput()
    {
        var recipe = CreateRecipe(ingredientCount: 300);

        var inner = new MemoryStream();
        await using var output = new TrackingStream(inner);

        await RecipeZipWriter.WriteRecipeAsync(output, recipe);

        Assert.True(output.WriteCount > 1,
            $"Expected archive to be written progressively, but saw only {output.WriteCount} write call(s).");

        inner.Position = 0;
        using var archive = new ZipArchive(inner, ZipArchiveMode.Read);
        Assert.Single(archive.Entries);
    }

    [Fact]
    public async Task WriteRecipeAsync_DoesNotCloseCallerStream()
    {
        var recipe = CreateRecipe();
        var inner = new MemoryStream();
        await using var output = new TrackingStream(inner);

        await RecipeZipWriter.WriteRecipeAsync(output, recipe);

        Assert.False(output.IsDisposed, "Caller-owned output stream must not be closed by the writer.");

        inner.Position = 0;
        using var archive = new ZipArchive(inner, ZipArchiveMode.Read);
        Assert.Single(archive.Entries);
    }

    [Fact]
    public async Task WriteRecipeAsync_PreCanceledToken_ThrowsAndLeavesOutputUntouched()
    {
        var recipe = CreateRecipe();
        var inner = new MemoryStream();
        await using var output = new TrackingStream(inner);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => RecipeZipWriter.WriteRecipeAsync(output, recipe, cts.Token));

        Assert.Equal(0, output.WriteCount);
        Assert.False(output.IsDisposed, "Caller-owned output stream must not be closed by the writer.");
    }

    [Fact]
    public async Task WriteRecipeAsync_CancelDuringOutput_PropagatesAndDoesNotCloseCallerStream()
    {
        var recipe = CreateRecipe(ingredientCount: 300);
        var inner = new MemoryStream();
        using var cts = new CancellationTokenSource();
        await using var output = new TrackingStream(inner, onFirstWrite: cts.Cancel);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => RecipeZipWriter.WriteRecipeAsync(output, recipe, cts.Token));

        Assert.False(output.IsDisposed, "Caller-owned output stream must not be closed by the writer.");
    }

    [Fact]
    public async Task WriteRecipeAsync_MatchesFormerBufferedSerialization_ForLoadedGraphWithCycles()
    {
        var recipe = CreateLoadedRecipeGraph();
        using var output = new MemoryStream();

        await RecipeZipWriter.WriteRecipeAsync(output, recipe);

        output.Position = 0;
        using var archive = new ZipArchive(output, ZipArchiveMode.Read);
        var entry = Assert.Single(archive.Entries);
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        var actual = await reader.ReadToEndAsync();

        var expected = JsonSerializer.Serialize(recipe, FormerSerializerOptions);

        Assert.Equal(expected, actual);
    }

    private static Recipe CreateRecipe(int ingredientCount = 1)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "Chicken Soup",
            Slug = "chicken-soup",
            Description = "A comforting bowl of soup.",
            RecipeYield = "4 servings",
            GroupId = Guid.NewGuid(),
            HouseholdId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        for (var i = 0; i < ingredientCount; i++)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                Position = i,
                OriginalText = ingredientCount == 1 ? "1 cup broth" : $"1 cup broth number {i}",
                RecipeId = recipe.Id
            });
        }

        return recipe;
    }

    /// <summary>
    ///     A graph mirroring a fully loaded EF entity graph: collections with back-references to
    ///     the recipe and organizers that reference the recipe, creating reference cycles.
    /// </summary>
    private static Recipe CreateLoadedRecipeGraph()
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "Loaded Graph Soup",
            Slug = "loaded-graph-soup",
            Description = "A recipe graph with back-references.",
            RecipeYield = "6 servings",
            GroupId = Guid.NewGuid(),
            HouseholdId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
            Nutrition = new Nutrition { Calories = "250", ProteinContent = "12 g" },
            Settings = new RecipeSettings { Public = true, ShowNutrition = true }
        };

        var ingredient = new RecipeIngredient
        {
            Id = Guid.NewGuid(),
            Position = 0,
            OriginalText = "2 cups vegetable stock",
            RecipeId = recipe.Id,
            Recipe = recipe
        };
        var instruction = new RecipeInstruction
        {
            Id = Guid.NewGuid(),
            Position = 0,
            Text = "Simmer for 20 minutes",
            Title = "Simmer",
            RecipeId = recipe.Id,
            Recipe = recipe
        };
        var note = new RecipeNote
        {
            Id = Guid.NewGuid(),
            Title = "Serve with",
            Text = "crusty bread",
            RecipeId = recipe.Id,
            Recipe = recipe
        };
        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = "Soup",
            Slug = "soup",
            GroupId = recipe.GroupId,
            CreatedAt = recipe.CreatedAt,
            UpdateAt = recipe.UpdateAt
        };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Dinner",
            Slug = "dinner",
            GroupId = recipe.GroupId,
            CreatedAt = recipe.CreatedAt,
            UpdateAt = recipe.UpdateAt
        };

        recipe.RecipeIngredients.Add(ingredient);
        recipe.RecipeInstructions.Add(instruction);
        recipe.Notes.Add(note);
        recipe.Tags.Add(tag);
        recipe.Categories.Add(category);
        tag.Recipes.Add(recipe);
        category.Recipes.Add(recipe);

        return recipe;
    }

    /// <summary>
    ///     A non-seekable stream that rejects every synchronous output operation, simulating an
    ///     ASP.NET Core response stream with synchronous IO disabled.
    /// </summary>
    private sealed class AsyncOnlyNonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new InvalidOperationException("Synchronous IO is not allowed.");

        public override Task FlushAsync(CancellationToken cancellationToken = default) =>
            inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new InvalidOperationException("Synchronous IO is not allowed.");

        public override void Write(ReadOnlySpan<byte> buffer) =>
            throw new InvalidOperationException("Synchronous IO is not allowed.");

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.WriteAsync(buffer, cancellationToken);
    }

    private sealed class TrackingStream(Stream inner, Action? onFirstWrite = null) : Stream
    {
        private bool _writeObserved;

        public int WriteCount { get; private set; }
        public bool IsDisposed { get; private set; }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) =>
            inner.Seek(offset, origin);

        public override void SetLength(long value) =>
            inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            NoteWrite();
            inner.Write(buffer, offset, count);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            NoteWrite();
            return inner.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }

        private void NoteWrite()
        {
            WriteCount++;
            if (_writeObserved)
            {
                return;
            }

            _writeObserved = true;
            onFirstWrite?.Invoke();
        }
    }
}
