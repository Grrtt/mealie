using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Mealie.IntegrationTests.Households;

/// <summary>
/// Isolated factory that uses a per-class in-memory SQLite database.
/// </summary>
public class MealPlanTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"mealie-test-{Guid.NewGuid():N}.db");
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), $"mealie-test-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        // Program.cs reads these directly from Environment.GetEnvironmentVariable
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
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
            if (Directory.Exists(_dataDir)) Directory.Delete(_dataDir, recursive: true);
        }
        foreach (var key in new[] { "DATABASE_URL", "DB_ENGINE", "SECRET", "ALLOW_SIGNUP", "DATA_DIR" })
            Environment.SetEnvironmentVariable(key, null);
    }
}

public class MealPlanIntegrationTests(MealPlanTestFactory factory)
    : IClassFixture<MealPlanTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── helpers ────────────────────────────────────────────────────────────────

    private async Task<string> LoginAsync()
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", Mealie.Api.Commands.SeedCommand.DefaultEmail),
            new KeyValuePair<string, string>("password", Mealie.Api.Commands.SeedCommand.DefaultPassword),
        });
        var response = await _client.PostAsync("/api/auth/token", form);
        Assert.True(response.IsSuccessStatusCode,
            $"Login failed with {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(_json);
        Assert.NotNull(body?.AccessToken);
        return body!.AccessToken;
    }

    private HttpClient AuthenticatedClient(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateThenDelete_MealPlan_Returns204()
    {
        var token = await LoginAsync();
        using var client = AuthenticatedClient(token);

        // Create a meal plan entry
        var createBody = new
        {
            title = "Test Plan",
            text = "",
            entry_type = "breakfast",
            date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            recipe_id = (string?)null
        };
        var createResp = await client.PostAsJsonAsync("/api/households/mealplans", createBody, _json);
        Assert.True(createResp.IsSuccessStatusCode,
            $"Create failed with {createResp.StatusCode}: {await createResp.Content.ReadAsStringAsync()}");

        var created = await createResp.Content.ReadFromJsonAsync<MealPlanEntry>(_json);
        Assert.NotNull(created);
        Assert.NotNull(created!.Id);
        Assert.NotEqual(Guid.Empty, Guid.Parse(created.Id));

        // Delete it using the returned ID
        var deleteResp = await client.DeleteAsync($"/api/households/mealplans/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    [Fact]
    public async Task DeleteNonExistentMealPlan_Returns404()
    {
        var token = await LoginAsync();
        using var client = AuthenticatedClient(token);

        var fakeId = Guid.NewGuid();
        var resp = await client.DeleteAsync($"/api/households/mealplans/{fakeId}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_AfterCreate_ReturnsCorrectPlan()
    {
        var token = await LoginAsync();
        using var client = AuthenticatedClient(token);

        var createBody = new
        {
            title = "Lunch Plan",
            text = "",
            entry_type = "lunch",
            date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            recipe_id = (string?)null
        };
        var createResp = await client.PostAsJsonAsync("/api/households/mealplans", createBody, _json);
        var created = await createResp.Content.ReadFromJsonAsync<MealPlanEntry>(_json);
        Assert.NotNull(created?.Id);

        var getResp = await client.GetAsync($"/api/households/mealplans/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
    }

    // ── DTOs ───────────────────────────────────────────────────────────────────

    private record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
    private record MealPlanEntry([property: JsonPropertyName("id")] string Id);
}
