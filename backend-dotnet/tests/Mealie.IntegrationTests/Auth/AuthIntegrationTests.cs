using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mealie.IntegrationTests.Auth;

public class AuthIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", "notauser@example.com"),
            new KeyValuePair<string, string>("password", "wrongpassword")
        });
        var response = await _client.PostAsync("/api/auth/token", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRegistrationInfo_ReturnsAllowSignup()
    {
        var response = await _client.GetAsync("/api/users/registration");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegistrationInfo>();
        Assert.NotNull(body);
    }

    private record RegistrationInfo(bool allow_registration);
}
