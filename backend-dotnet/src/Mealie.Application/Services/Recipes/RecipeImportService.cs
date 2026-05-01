using System.IO.Compression;
using System.Text.Json;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Recipes;

public class RecipeImportService(ApplicationDbContext db) : IRecipeImportService
{
    public async Task<int> ImportFromZipAsync(Stream zipStream, Guid householdId, Guid groupId,
        CancellationToken ct = default)
    {
        var imported = 0;
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, true);

        foreach (var entry in archive.Entries)
        {
            if (!entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                await using var stream = entry.Open();
                var recipe = await JsonSerializer.DeserializeAsync<Recipe>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }, ct);

                if (recipe is null)
                {
                    continue;
                }

                recipe.Id = Guid.NewGuid();
                recipe.GroupId = groupId;
                recipe.HouseholdId = householdId;
                recipe.CreatedAt = DateTime.UtcNow;
                recipe.UpdateAt = DateTime.UtcNow;

                var slug = recipe.Slug;
                var counter = 1;
                while (await db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.Slug == slug, ct))
                {
                    slug = $"{recipe.Slug}-{counter++}";
                }

                recipe.Slug = slug;

                foreach (var ingredient in recipe.RecipeIngredients)
                {
                    ingredient.Id = Guid.NewGuid();
                }

                foreach (var instruction in recipe.RecipeInstructions)
                {
                    instruction.Id = Guid.NewGuid();
                }

                foreach (var note in recipe.Notes)
                {
                    note.Id = Guid.NewGuid();
                }

                db.Recipes.Add(recipe);
                await db.SaveChangesAsync(ct);
                imported++;
            }
            catch
            {
                // Skip malformed entries
            }
        }

        return imported;
    }
}
