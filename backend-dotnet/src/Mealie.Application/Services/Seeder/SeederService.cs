using System.Reflection;
using System.Text.Json;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Seeder;

public class SeederService(ApplicationDbContext db) : ISeederService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // ── Foods JSON shape ─────────────────────────────────────────────────────
    // { "Label Name": { "foods": { "food key": { "name": "...", "plural_name": "..." } } } }

    // ── Units JSON shape ─────────────────────────────────────────────────────
    // { "unit key": { "name": "...", "plural_name": "...", "description": "...", "abbreviation": "...", "plural_abbreviation": "..." } }

    public async Task SeedLabelsAsync(Guid groupId, string locale, CancellationToken ct = default)
    {
        var foodsJson = LoadFoodsJson(locale);

        var existingNames = await db.Labels
            .IgnoreQueryFilters()
            .Where(l => l.GroupId == groupId)
            .Select(l => l.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, ct);

        var now = DateTime.UtcNow;
        foreach (var labelName in foodsJson.Keys)
        {
            if (string.IsNullOrWhiteSpace(labelName) || existingNames.Contains(labelName))
            {
                continue;
            }

            db.Labels.Add(new MultiPurposeLabel
            {
                Id = Guid.NewGuid(),
                Name = labelName,
                GroupId = groupId,
                CreatedAt = now,
                UpdateAt = now
            });
            existingNames.Add(labelName);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task SeedFoodsAsync(Guid groupId, string locale, CancellationToken ct = default)
    {
        var foodsJson = LoadFoodsJson(locale);

        var existingFoodNames = await db.Foods
            .IgnoreQueryFilters()
            .Where(f => f.GroupId == groupId)
            .Select(f => f.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, ct);

        var labelsByName = await db.Labels
            .IgnoreQueryFilters()
            .Where(l => l.GroupId == groupId)
            .ToDictionaryAsync(l => l.Name, l => l.Id, StringComparer.OrdinalIgnoreCase, ct);

        var now = DateTime.UtcNow;
        foreach (var (labelName, category) in foodsJson)
        {
            labelsByName.TryGetValue(labelName, out var labelId);

            foreach (var (_, foodData) in category.Foods)
            {
                var foodName = foodData.Name;
                if (string.IsNullOrWhiteSpace(foodName) || existingFoodNames.Contains(foodName))
                {
                    continue;
                }

                db.Foods.Add(new IngredientFood
                {
                    Id = Guid.NewGuid(),
                    Name = foodName,
                    PluralName = foodData.PluralName,
                    Description = string.Empty,
                    GroupId = groupId,
                    LabelId = labelId == Guid.Empty ? null : labelId,
                    CreatedAt = now,
                    UpdateAt = now
                });
                existingFoodNames.Add(foodName);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task SeedUnitsAsync(Guid groupId, string locale, CancellationToken ct = default)
    {
        var unitsJson = LoadUnitsJson(locale);

        var existingNames = await db.Units
            .IgnoreQueryFilters()
            .Where(u => u.GroupId == groupId)
            .Select(u => u.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, ct);

        var now = DateTime.UtcNow;
        foreach (var (_, unitData) in unitsJson)
        {
            if (string.IsNullOrWhiteSpace(unitData.Name) || existingNames.Contains(unitData.Name))
            {
                continue;
            }

            db.Units.Add(new IngredientUnit
            {
                Id = Guid.NewGuid(),
                Name = unitData.Name,
                PluralName = unitData.PluralName,
                Description = unitData.Description ?? string.Empty,
                Abbreviation = unitData.Abbreviation,
                PluralAbbreviation = unitData.PluralAbbreviation,
                GroupId = groupId,
                CreatedAt = now,
                UpdateAt = now
            });
            existingNames.Add(unitData.Name);
        }

        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Dictionary<string, FoodCategory> LoadFoodsJson(string locale)
    {
        var stream = OpenLocaleResource("foods", locale);
        return JsonSerializer.Deserialize<Dictionary<string, FoodCategory>>(stream, JsonOpts)
               ?? [];
    }

    private Dictionary<string, UnitEntry> LoadUnitsJson(string locale)
    {
        var stream = OpenLocaleResource("units", locale);
        return JsonSerializer.Deserialize<Dictionary<string, UnitEntry>>(stream, JsonOpts)
               ?? [];
    }

    private static Stream OpenLocaleResource(string resourceType, string locale)
    {
        var assembly = Assembly.GetExecutingAssembly();
        // Embedded resource names use dots as path separators and match the project namespace
        var prefix = $"Mealie.Application.Resources.Seed.{resourceType}.locales.";
        var resourceName = $"{prefix}{locale}.json";

        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            return stream;
        }

        // Fall back to en-US if the requested locale isn't bundled
        stream = assembly.GetManifestResourceStream($"{prefix}en-US.json");
        return stream ?? throw new InvalidOperationException($"Seed data for '{resourceType}' not found.");
    }

    // ── JSON DTOs ─────────────────────────────────────────────────────────────

    private sealed record FoodCategory(
        Dictionary<string, FoodEntry> Foods);

    private sealed record FoodEntry(
        string Name,
        string? PluralName,
        string? Description);

    private sealed record UnitEntry(
        string Name,
        string? PluralName,
        string? Description,
        string? Abbreviation,
        string? PluralAbbreviation);
}
