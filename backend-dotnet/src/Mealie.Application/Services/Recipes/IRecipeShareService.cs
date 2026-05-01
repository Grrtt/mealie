using Mealie.Application.Dtos.Recipes;

namespace Mealie.Application.Services.Recipes;

public interface IRecipeShareService
{
    Task<IList<ShareTokenResponse>> GetShareTokensAsync(string slug, CancellationToken ct = default);

    Task<ShareTokenResponse?> CreateShareTokenAsync(string slug, Guid groupId, CreateShareTokenRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteShareTokenAsync(Guid tokenId, CancellationToken ct = default);
    Task<ShareTokenResponse?> GetShareTokenAsync(Guid tokenId, CancellationToken ct = default);
}
