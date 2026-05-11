using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Api.Commands;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Mealie.IntegrationTests.Recipes;

public class RecipeDeleteTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), $"mealie-test-{Guid.NewGuid():N}");
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"mealie-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("DATABASE_URL", $"Data Source={_dbPath}");
        Environment.SetEnvironmentVariable("DB_ENGINE", "sqlite");
        Environment.SetEnvironmentVariable("SECRET", "test-secret-value-that-is-32-chars!!");
        Environment.SetEnvironmentVariable("ALLOW_SIGNUP", "true");
        Environment.SetEnvironmentVariable("DATA_DIR", _dataDir);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }

            if (Directory.Exists(_dataDir))
            {
                Directory.Delete(_dataDir, true);
            }
        }

        foreach (var key in new[] { "DATABASE_URL", "DB_ENGINE", "SECRET", "ALLOW_SIGNUP", "DATA_DIR" })
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }
}

public class RecipeDeleteIntegrationTests(RecipeDeleteTestFactory factory) : IClassFixture<RecipeDeleteTestFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> LoginAsync()
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", SeedCommand.DefaultEmail),
            new KeyValuePair<string, string>("password", SeedCommand.DefaultPassword)
        });
        var response = await _client.PostAsync("/api/auth/token", form);
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

    [Fact]
    public async Task DeleteRecipe_WithInstructions_ReturnsOk()
    {
        var token = await LoginAsync();
        using var client = AuthenticatedClient(token);

        var createResponse = await client.PostAsJsonAsync("/api/recipes", new { name = "Delete Test Recipe" }, Json);
        Assert.True(createResponse.IsSuccessStatusCode,
            $"Create failed with {createResponse.StatusCode}: {await createResponse.Content.ReadAsStringAsync()}");

        var created = await createResponse.Content.ReadFromJsonAsync<RecipeResponse>(Json);
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created!.Slug));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.RecipeInstructions.Add(new RecipeInstruction
            {
                Id = Guid.NewGuid(),
                RecipeId = created.Id,
                Position = 0,
                Text = "Mix everything together.",
                Title = "Step 1",
                Summary = "Mix"
            });
            await db.SaveChangesAsync();
        }

        var deleteResponse = await client.DeleteAsync($"/api/recipes/{created.Slug}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    private record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);

    private record RecipeResponse(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("slug")] string Slug);
}
