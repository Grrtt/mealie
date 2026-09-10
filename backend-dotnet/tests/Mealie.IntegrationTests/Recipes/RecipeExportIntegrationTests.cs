using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Api.Commands;
using Mealie.Api.Controllers.Recipes;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mealie.IntegrationTests.Recipes;

public class RecipeExportIntegrationTests(RecipeDeleteTestFactory factory) : IClassFixture<RecipeDeleteTestFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public async Task ExportRecipe_ReturnsZipAttachmentContainingRecipeJson()
    {
        var token = await LoginAsync();
        using var client = AuthenticatedClient(token);

        var createResponse = await client.PostAsJsonAsync("/api/recipes", new { name = "Export Test Recipe" }, Json);
        Assert.True(createResponse.IsSuccessStatusCode,
            $"Create failed with {createResponse.StatusCode}: {await createResponse.Content.ReadAsStringAsync()}");

        var created = await createResponse.Content.ReadFromJsonAsync<RecipeIdentity>(Json);
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created!.Slug));

        var updateResponse = await client.PatchAsJsonAsync($"/api/recipes/{created.Slug}", new
        {
            recipeIngredients = new[]
            {
                new { position = 0, originalText = "1 cup milk", disableAmount = false }
            }
        }, Json);
        Assert.True(updateResponse.IsSuccessStatusCode,
            $"Update failed with {updateResponse.StatusCode}: {await updateResponse.Content.ReadAsStringAsync()}");

        var exportResponse = await client.GetAsync($"/api/recipes/{created.Slug}/exports");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal("application/zip", exportResponse.Content.Headers.ContentType?.MediaType);

        var disposition = exportResponse.Content.Headers.ContentDisposition;
        Assert.NotNull(disposition);
        Assert.Equal("attachment", disposition!.DispositionType);
        Assert.Equal($"{created.Slug}.zip", disposition.FileName);

        var zipBytes = await exportResponse.Content.ReadAsByteArrayAsync();
        using var archive = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal($"{created.Slug}/{created.Slug}.json", entry.FullName);

        using var entryStream = entry.Open();
        using var doc = await JsonDocument.ParseAsync(entryStream);
        var root = doc.RootElement;
        Assert.Equal("Export Test Recipe", root.GetProperty("Name").GetString());
        Assert.Equal(created.Slug, root.GetProperty("Slug").GetString());

        var ingredient = Assert.Single(root.GetProperty("RecipeIngredients").EnumerateArray());
        Assert.Equal("1 cup milk", ingredient.GetProperty("OriginalText").GetString());
    }

    [Fact]
    public async Task ExportRecipe_MissingRecipe_ReturnsNotFound()
    {
        var token = await LoginAsync();
        using var client = AuthenticatedClient(token);

        var response = await client.GetAsync("/api/recipes/does-not-exist/exports");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExportRecipe_RecipeFromOtherHousehold_ReturnsNotFound()
    {
        const string otherSlug = "other-household-recipe";
        await SeedRecipeInOtherHouseholdAsync(otherSlug);

        var token = await LoginAsync();
        using var client = AuthenticatedClient(token);

        var response = await client.GetAsync($"/api/recipes/{otherSlug}/exports");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExportRecipe_Unauthenticated_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/recipes/any-recipe/exports");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExportHeaders_MatchFileResultForAsciiAndUtf8FileNames()
    {
        foreach (var fileName in new[] { "plain-name.zip", "食谱-名.zip" })
        {
            var actualContext = new DefaultHttpContext();
            RecipeExportResponseHeaders.Apply(actualContext.Response, fileName);

            var expectedContext = new DefaultHttpContext { RequestServices = factory.Services };
            var fileResult = new FileContentResult([], "application/zip") { FileDownloadName = fileName };
            await fileResult.ExecuteResultAsync(
                new ActionContext(expectedContext, new RouteData(), new ActionDescriptor()));

            Assert.Equal(expectedContext.Response.ContentType, actualContext.Response.ContentType);
            Assert.Equal(expectedContext.Response.Headers.ContentDisposition,
                actualContext.Response.Headers.ContentDisposition);
        }
    }

    private async Task SeedRecipeInOtherHouseholdAsync(string slug)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == SeedCommand.DefaultEmail);
        var now = DateTime.UtcNow;

        var otherHousehold = new Household
        {
            Id = Guid.NewGuid(),
            Name = "Other Household",
            Slug = "other-household",
            GroupId = user.GroupId,
            CreatedAt = now,
            UpdateAt = now
        };
        db.Households.Add(otherHousehold);
        db.Recipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "Other Household Recipe",
            Slug = slug,
            GroupId = user.GroupId,
            HouseholdId = otherHousehold.Id,
            CreatedAt = now,
            UpdateAt = now
        });
        await db.SaveChangesAsync();
    }

    private async Task<string> LoginAsync()
    {
        using var client = factory.CreateClient();
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", SeedCommand.DefaultEmail),
            new KeyValuePair<string, string>("password", SeedCommand.DefaultPassword)
        });
        var response = await client.PostAsync("/api/auth/token", form);
        Assert.True(response.IsSuccessStatusCode,
            $"Login failed with {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(Json);
        Assert.NotNull(body?.AccessToken);
        return body!.AccessToken;
    }

    private HttpClient AuthenticatedClient(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);

    private record RecipeIdentity(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("slug")] string Slug);
}
