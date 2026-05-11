using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Api.Commands;
using Mealie.Api.Mcp;
using Mealie.IntegrationTests.Households;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Serilog.Parsing;

namespace Mealie.IntegrationTests.Admin;

public class AdminLogsIntegrationTests(MealPlanTestFactory factory)
    : IClassFixture<MealPlanTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAdminLogs_WithoutAuthentication_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/logs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminLogs_ReturnsRecentWarningAndErrorEntries()
    {
        var infoMarker = Guid.NewGuid().ToString("N");
        var warningMarker = Guid.NewGuid().ToString("N");
        var errorMarker = Guid.NewGuid().ToString("N");

        using (var scope = factory.Services.CreateScope())
        {
            var logStore = scope.ServiceProvider.GetRequiredService<InMemoryLogStore>();
            logStore.Emit(CreateLogEvent(LogEventLevel.Information, $"Admin logs info marker {infoMarker}"));
            logStore.Emit(CreateLogEvent(LogEventLevel.Warning, $"Admin logs warning marker {warningMarker}"));
            logStore.Emit(CreateLogEvent(LogEventLevel.Error, $"Admin logs error marker {errorMarker}"));
        }

        var token = await LoginAsync();
        using var client = AuthenticatedClient(token);

        var response = await client.GetAsync("/api/admin/logs?minimumLevel=Warning&limit=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AdminLogsResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Warning", body!.MinimumLevel);
        Assert.True(body.TotalCount >= body.Entries.Count);
        Assert.Contains(body.Entries, entry => entry.Message.Contains(warningMarker, StringComparison.Ordinal));
        Assert.Contains(body.Entries, entry => entry.Message.Contains(errorMarker, StringComparison.Ordinal));
        Assert.DoesNotContain(body.Entries, entry => entry.Message.Contains(infoMarker, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetAdminLogs_WithErrorMinimumLevel_ExcludesWarnings()
    {
        var warningMarker = Guid.NewGuid().ToString("N");
        var errorMarker = Guid.NewGuid().ToString("N");

        using (var scope = factory.Services.CreateScope())
        {
            var logStore = scope.ServiceProvider.GetRequiredService<InMemoryLogStore>();
            logStore.Emit(CreateLogEvent(LogEventLevel.Warning, $"Admin logs filtered warning marker {warningMarker}"));
            logStore.Emit(CreateLogEvent(LogEventLevel.Error, $"Admin logs filtered error marker {errorMarker}"));
        }

        var token = await LoginAsync();
        using var client = AuthenticatedClient(token);

        var response = await client.GetAsync("/api/admin/logs?minimumLevel=Error&limit=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AdminLogsResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Error", body!.MinimumLevel);
        Assert.Contains(body.Entries, entry => entry.Message.Contains(errorMarker, StringComparison.Ordinal));
        Assert.DoesNotContain(body.Entries, entry => entry.Message.Contains(warningMarker, StringComparison.Ordinal));
    }

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

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        Assert.NotNull(body?.AccessToken);
        return body!.AccessToken;
    }

    private HttpClient AuthenticatedClient(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static LogEvent CreateLogEvent(LogEventLevel level, string message)
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            level,
            exception: null,
            new MessageTemplateParser().Parse(message),
            []);
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")]
        string AccessToken);

    private sealed record AdminLogsResponse(
        string MinimumLevel,
        int Limit,
        int TotalCount,
        List<AdminLogEntryResponse> Entries);

    private sealed record AdminLogEntryResponse(
        DateTime Timestamp,
        string Level,
        string Message,
        string? Exception,
        string? SourceContext,
        string? RequestMethod,
        string? RequestPath,
        int? StatusCode,
        string? CorrelationId);
}
