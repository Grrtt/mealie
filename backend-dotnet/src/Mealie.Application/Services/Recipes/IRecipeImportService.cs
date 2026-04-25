namespace Mealie.Application.Services.Recipes;

public interface IRecipeImportService
{
    Task<int> ImportFromZipAsync(Stream zipStream, Guid householdId, Guid groupId, CancellationToken ct = default);
}
