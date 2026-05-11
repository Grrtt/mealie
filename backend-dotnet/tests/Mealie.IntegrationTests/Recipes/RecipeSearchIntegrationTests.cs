using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Api.Commands;

namespace Mealie.IntegrationTests.Recipes;

public class RecipeSearchIntegrationTests(RecipeDeleteTestFactory factory) : IClassFixture<RecipeDeleteTestFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public async Task SearchRecipes_WithPartialTerm_FallsBackToSubstringMatch()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/recipes", new { name = "Korean Beef" }, Json);
        Assert.True(createResponse.IsSuccessStatusCode,
            $"Create failed with {createResponse.StatusCode}: {await createResponse.Content.ReadAsStringAsync()}");

        var searchResponse = await client.GetAsync("/api/recipes?search=korea&page=1&perPage=24");
        Assert.True(searchResponse.IsSuccessStatusCode,
            $"Search failed with {searchResponse.StatusCode}: {await searchResponse.Content.ReadAsStringAsync()}");

        var page = await searchResponse.Content.ReadFromJsonAsync<RecipeSearchPage>(Json);
        Assert.NotNull(page);
        Assert.Contains(page!.Items, recipe => recipe.Name == "Korean Beef");
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

    private record RecipeSearchPage([property: JsonPropertyName("items")] List<RecipeSearchItem> Items);

    private record RecipeSearchItem([property: JsonPropertyName("name")] string Name);
}
