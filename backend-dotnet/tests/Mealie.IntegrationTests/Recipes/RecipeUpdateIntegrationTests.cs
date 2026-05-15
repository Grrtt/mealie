using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Api.Commands;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mealie.IntegrationTests.Recipes;

public class RecipeUpdateIntegrationTests(RecipeDeleteTestFactory factory) : IClassFixture<RecipeDeleteTestFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public async Task UpdateRecipe_ReplacesCollectionsAndOrganizerLinks()
    {
        using var client = await CreateAuthenticatedClientAsync();
        await SeedOrganizersAsync();

        var createResponse = await client.PostAsJsonAsync("/api/recipes", new { name = "Update Test Recipe" }, Json);
        Assert.True(createResponse.IsSuccessStatusCode,
            $"Create failed with {createResponse.StatusCode}: {await createResponse.Content.ReadAsStringAsync()}");

        var created = await createResponse.Content.ReadFromJsonAsync<RecipeIdentityResponse>(Json);
        Assert.NotNull(created);

        var firstUpdate = await client.PatchAsJsonAsync($"/api/recipes/{created!.Slug}", new
        {
            name = "Update Test Recipe v1",
            recipeIngredients = new[]
            {
                new { position = 0, originalText = "1 cup milk", disableAmount = false }
            },
            recipeInstructions = new[]
            {
                new { position = 0, text = "Whisk everything together" }
            },
            notes = new[]
            {
                new { title = "Note 1", text = "Serve warm" }
            },
            tags = new[] { "tag-one" },
            categories = new[] { "category-one" },
            tools = new[] { "tool-one" }
        }, Json);
        Assert.True(firstUpdate.IsSuccessStatusCode,
            $"First update failed with {firstUpdate.StatusCode}: {await firstUpdate.Content.ReadAsStringAsync()}");

        var secondUpdate = await client.PatchAsJsonAsync($"/api/recipes/{created.Slug}", new
        {
            name = "Update Test Recipe v2",
            recipeIngredients = new[]
            {
                new { position = 0, originalText = "2 cups broth", disableAmount = false }
            },
            recipeInstructions = new[]
            {
                new { position = 0, text = "Simmer for 10 minutes" }
            },
            notes = new[]
            {
                new { title = "Note 2", text = "Finish with herbs" }
            },
            tags = new[] { "tag-two" },
            categories = new[] { "category-two" },
            tools = new[] { "tool-two" }
        }, Json);
        Assert.True(secondUpdate.IsSuccessStatusCode,
            $"Second update failed with {secondUpdate.StatusCode}: {await secondUpdate.Content.ReadAsStringAsync()}");

        var updated = await secondUpdate.Content.ReadFromJsonAsync<RecipeDetailResponse>(Json);
        Assert.NotNull(updated);
        Assert.Equal("Update Test Recipe v2", updated!.Name);
        Assert.Collection(updated.RecipeIngredients, ingredient => Assert.Equal("2 cups broth", ingredient.OriginalText));
        Assert.Collection(updated.RecipeInstructions, instruction => Assert.Equal("Simmer for 10 minutes", instruction.Text));
        Assert.Collection(updated.Notes, note => Assert.Equal("Finish with herbs", note.Text));
        Assert.Collection(updated.Tags, tag => Assert.Equal("tag-two", tag.Slug));
        Assert.Collection(updated.Categories, category => Assert.Equal("category-two", category.Slug));
        Assert.Collection(updated.Tools, tool => Assert.Equal("tool-two", tool.Slug));

        var persistedResponse = await client.GetAsync($"/api/recipes/{created.Slug}");
        Assert.True(persistedResponse.IsSuccessStatusCode,
            $"Get failed with {persistedResponse.StatusCode}: {await persistedResponse.Content.ReadAsStringAsync()}");

        var persisted = await persistedResponse.Content.ReadFromJsonAsync<RecipeDetailResponse>(Json);
        Assert.NotNull(persisted);
        Assert.Collection(persisted!.RecipeIngredients, ingredient => Assert.Equal("2 cups broth", ingredient.OriginalText));
        Assert.Collection(persisted.Tags, tag => Assert.Equal("tag-two", tag.Slug));
        Assert.Collection(persisted.Categories, category => Assert.Equal("category-two", category.Slug));
        Assert.Collection(persisted.Tools, tool => Assert.Equal("tool-two", tool.Slug));
    }

    private async Task SeedOrganizersAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == SeedCommand.DefaultEmail);
        var now = DateTime.UtcNow;

        db.Tags.AddRange(
            new Tag
            {
                Id = Guid.NewGuid(),
                GroupId = user.GroupId,
                Name = "Tag One",
                Slug = "tag-one",
                CreatedAt = now,
                UpdateAt = now
            },
            new Tag
            {
                Id = Guid.NewGuid(),
                GroupId = user.GroupId,
                Name = "Tag Two",
                Slug = "tag-two",
                CreatedAt = now,
                UpdateAt = now
            });

        db.Categories.AddRange(
            new Category
            {
                Id = Guid.NewGuid(),
                GroupId = user.GroupId,
                Name = "Category One",
                Slug = "category-one",
                CreatedAt = now,
                UpdateAt = now
            },
            new Category
            {
                Id = Guid.NewGuid(),
                GroupId = user.GroupId,
                Name = "Category Two",
                Slug = "category-two",
                CreatedAt = now,
                UpdateAt = now
            });

        db.Tools.AddRange(
            new Tool
            {
                Id = Guid.NewGuid(),
                GroupId = user.GroupId,
                Name = "Tool One",
                Slug = "tool-one",
                OnHand = false,
                CreatedAt = now,
                UpdateAt = now
            },
            new Tool
            {
                Id = Guid.NewGuid(),
                GroupId = user.GroupId,
                Name = "Tool Two",
                Slug = "tool-two",
                OnHand = false,
                CreatedAt = now,
                UpdateAt = now
            });

        await db.SaveChangesAsync();
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

    private record RecipeIdentityResponse(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("slug")] string Slug);

    private record RecipeDetailResponse(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("recipeIngredient")] List<RecipeIngredientResponse> RecipeIngredients,
        [property: JsonPropertyName("recipeInstructions")] List<RecipeInstructionResponse> RecipeInstructions,
        [property: JsonPropertyName("notes")] List<RecipeNoteResponse> Notes,
        [property: JsonPropertyName("tags")] List<OrganizerResponse> Tags,
        [property: JsonPropertyName("recipeCategory")] List<OrganizerResponse> Categories,
        [property: JsonPropertyName("tools")] List<OrganizerResponse> Tools);

    private record RecipeIngredientResponse([property: JsonPropertyName("originalText")] string? OriginalText);

    private record RecipeInstructionResponse([property: JsonPropertyName("text")] string Text);

    private record RecipeNoteResponse([property: JsonPropertyName("text")] string Text);

    private record OrganizerResponse([property: JsonPropertyName("slug")] string Slug);
}
