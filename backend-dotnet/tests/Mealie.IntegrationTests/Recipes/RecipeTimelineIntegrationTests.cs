using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Api.Commands;

namespace Mealie.IntegrationTests.Recipes;

public class RecipeTimelineIntegrationTests(RecipeDeleteTestFactory factory)
    : IClassFixture<RecipeDeleteTestFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public async Task CreateRecipe_AddsCreatedEventToTimeline()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/recipes", new { name = "Timeline Test Recipe" }, Json);
        Assert.True(createResponse.IsSuccessStatusCode,
            $"Create failed with {createResponse.StatusCode}: {await createResponse.Content.ReadAsStringAsync()}");

        var created = await createResponse.Content.ReadFromJsonAsync<RecipeDetailResponse>(Json);
        Assert.NotNull(created);

        var timelineResponse = await client.GetAsync($"/api/recipes/{created!.Slug}/timeline");
        Assert.True(timelineResponse.IsSuccessStatusCode,
            $"Timeline failed with {timelineResponse.StatusCode}: {await timelineResponse.Content.ReadAsStringAsync()}");

        var events = await timelineResponse.Content.ReadFromJsonAsync<List<TimelineEventResponse>>(Json);
        Assert.NotNull(events);
        var createdEvent = Assert.Single(events);

        Assert.Equal(created.Id, createdEvent.RecipeId);
        Assert.Equal("Timeline Test Recipe", createdEvent.Subject);
        Assert.Equal("system", createdEvent.EventType);
        Assert.Equal("Recipe created", createdEvent.EventMessage);
        Assert.NotNull(createdEvent.UserId);
        Assert.Equal(created.Slug, createdEvent.RecipeSlug);
        Assert.Equal("Timeline Test Recipe", createdEvent.RecipeName);
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
        [property: JsonPropertyName("slug")] string Slug);

    private record TimelineEventResponse(
        [property: JsonPropertyName("recipeId")] Guid RecipeId,
        [property: JsonPropertyName("userId")] Guid? UserId,
        [property: JsonPropertyName("subject")] string? Subject,
        [property: JsonPropertyName("eventType")] string? EventType,
        [property: JsonPropertyName("eventMessage")] string? EventMessage,
        [property: JsonPropertyName("recipeSlug")] string? RecipeSlug,
        [property: JsonPropertyName("recipeName")] string? RecipeName);
}
