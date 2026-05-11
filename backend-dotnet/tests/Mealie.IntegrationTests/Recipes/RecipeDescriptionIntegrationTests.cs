using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Api.Commands;
using Mealie.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Mealie.IntegrationTests.Recipes;

public class RecipeDescriptionIntegrationTests(RecipeDeleteTestFactory factory) : IClassFixture<RecipeDeleteTestFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public async Task RecipeResponses_DecodeHtmlEntitiesInDescriptions()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/recipes", new
        {
            name = "Entity Test Recipe",
        }, Json);
        Assert.True(createResponse.IsSuccessStatusCode,
            $"Create failed with {createResponse.StatusCode}: {await createResponse.Content.ReadAsStringAsync()}");

        var created = await createResponse.Content.ReadFromJsonAsync<RecipeDetailResponse>(Json);
        Assert.NotNull(created);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var recipe = await db.Recipes.FindAsync(created!.Id);
            Assert.NotNull(recipe);
            recipe!.Description = "Mom&#39;s favorite bowl";
            await db.SaveChangesAsync();
        }

        var detailResponse = await client.GetAsync($"/api/recipes/{created.Slug}");
        Assert.True(detailResponse.IsSuccessStatusCode,
            $"Detail failed with {detailResponse.StatusCode}: {await detailResponse.Content.ReadAsStringAsync()}");

        var detail = await detailResponse.Content.ReadFromJsonAsync<RecipeDetailResponse>(Json);
        Assert.NotNull(detail);
        Assert.Equal("Mom's favorite bowl", detail!.Description);

        var listResponse = await client.GetAsync("/api/recipes?page=1&perPage=24");
        Assert.True(listResponse.IsSuccessStatusCode,
            $"List failed with {listResponse.StatusCode}: {await listResponse.Content.ReadAsStringAsync()}");

        var page = await listResponse.Content.ReadFromJsonAsync<RecipeListResponse>(Json);
        Assert.NotNull(page);

        var listedRecipe = Assert.Single(page!.Items.Where(recipe => recipe.Slug == created.Slug));
        Assert.Equal("Mom's favorite bowl", listedRecipe.Description);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var loginClient = factory.CreateClient();
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", SeedCommand.DefaultEmail),
            new KeyValuePair<string, string>("password", SeedCommand.DefaultPassword)
        });
        var response = await loginClient.PostAsync("/api/auth/token", form);
        Assert.True(response.IsSuccessStatusCode,
            $"Login failed with {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(Json);
        Assert.NotNull(body?.AccessToken);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    private record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);

    private record RecipeDetailResponse(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("slug")] string Slug,
        [property: JsonPropertyName("description")] string? Description);

    private record RecipeListResponse([property: JsonPropertyName("items")] List<RecipeSummaryResponse> Items);

    private record RecipeSummaryResponse(
        [property: JsonPropertyName("slug")] string Slug,
        [property: JsonPropertyName("description")] string? Description);
}
