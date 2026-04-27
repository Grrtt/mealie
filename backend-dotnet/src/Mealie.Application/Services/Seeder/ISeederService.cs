namespace Mealie.Application.Services.Seeder;

public interface ISeederService
{
    Task SeedFoodsAsync(Guid groupId, string locale, CancellationToken ct = default);
    Task SeedLabelsAsync(Guid groupId, string locale, CancellationToken ct = default);
    Task SeedUnitsAsync(Guid groupId, string locale, CancellationToken ct = default);
}
