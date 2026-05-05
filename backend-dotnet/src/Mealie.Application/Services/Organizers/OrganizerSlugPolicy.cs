using Mealie.Application.Common;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Organizers;

public static class OrganizerSlugPolicy
{
    public static Task<string> EnsureTagSlugAsync(
        ApplicationDbContext db,
        string name,
        Guid groupId,
        Guid? existingId = null,
        CancellationToken ct = default)
    {
        return EnsureUniqueAsync(
            SlugHelper.Generate(name),
            candidate => db.Tags.IgnoreQueryFilters()
                .AnyAsync(t => t.GroupId == groupId && t.Slug == candidate && (!existingId.HasValue || t.Id != existingId.Value), ct));
    }

    public static Task<string> EnsureCategorySlugAsync(
        ApplicationDbContext db,
        string name,
        Guid groupId,
        Guid? existingId = null,
        CancellationToken ct = default)
    {
        return EnsureUniqueAsync(
            SlugHelper.Generate(name),
            candidate => db.Categories.IgnoreQueryFilters()
                .AnyAsync(c => c.GroupId == groupId && c.Slug == candidate && (!existingId.HasValue || c.Id != existingId.Value), ct));
    }

    public static Task<string> EnsureToolSlugAsync(
        ApplicationDbContext db,
        string name,
        Guid groupId,
        Guid? existingId = null,
        CancellationToken ct = default)
    {
        return EnsureUniqueAsync(
            SlugHelper.Generate(name),
            candidate => db.Tools.IgnoreQueryFilters()
                .AnyAsync(t => t.GroupId == groupId && t.Slug == candidate && (!existingId.HasValue || t.Id != existingId.Value), ct));
    }

    private static async Task<string> EnsureUniqueAsync(string slug, Func<string, Task<bool>> slugExistsAsync)
    {
        var candidate = slug;
        var counter = 1;
        while (await slugExistsAsync(candidate))
        {
            candidate = $"{slug}-{counter++}";
        }

        return candidate;
    }
}
