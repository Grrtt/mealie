using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mealie.Api.Commands;
using Mealie.Application.Services.Auth;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mealie.IntegrationTests.Auth;

public class AuthTestFactory : WebApplicationFactory<Program>
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
        Environment.SetEnvironmentVariable("BASE_URL", "http://localhost");
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

        foreach (var key in new[] { "DATABASE_URL", "DB_ENGINE", "SECRET", "ALLOW_SIGNUP", "DATA_DIR", "BASE_URL" })
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }
}

public class AuthIntegrationTests(AuthTestFactory factory)
    : IClassFixture<AuthTestFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        using var client = factory.CreateClient();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", "notauser@example.com"),
            new KeyValuePair<string, string>("password", "wrongpassword")
        });
        var response = await client.PostAsync("/api/auth/token", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRegistrationInfo_ReturnsAllowSignup()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/users/registration");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegistrationInfo>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task Login_WithValidCredentials_SetsCookie_AndAuthenticatesSelf()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsync("/api/auth/token", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", SeedCommand.DefaultEmail),
            new KeyValuePair<string, string>("password", SeedCommand.DefaultPassword)
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders));
        Assert.Contains(cookieHeaders!, value => value.Contains("mealie.access_token=", StringComparison.Ordinal));
        Assert.Contains(cookieHeaders!, value => value.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var me = await client.GetAsync("/api/users/self");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var body = await me.Content.ReadFromJsonAsync<PrivateUserResponse>(Json);
        Assert.NotNull(body);
        Assert.Equal(SeedCommand.DefaultEmail, body!.Email);
    }

    [Fact]
    public async Task Register_WithInvite_AutoAuthenticates_AndSetsCookie()
    {
        using var client = factory.CreateClient();

        string invite;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var inviteService = scope.ServiceProvider.GetRequiredService<IRegistrationInviteService>();
            var defaultUser = await db.Users.SingleAsync(u => u.Email == SeedCommand.DefaultEmail);
            var rawToken = $"invite-{Guid.NewGuid():N}";

            db.InviteTokens.Add(new GroupInviteToken
            {
                Id = Guid.NewGuid(),
                Token = rawToken,
                GroupId = defaultUser.GroupId,
                HouseholdId = defaultUser.HouseholdId,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            invite = inviteService.CreateInvite("invitee@example.com", rawToken);
        }

        var response = await client.PostAsJsonAsync("/api/users/register", new
        {
            email = "invitee@example.com",
            username = $"invitee-{Guid.NewGuid():N}",
            fullName = "Invitee User",
            password = "supersecure123",
            invite
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders));
        Assert.Contains(cookieHeaders!, value => value.Contains("mealie.access_token=", StringComparison.Ordinal));

        var body = await response.Content.ReadFromJsonAsync<RegistrationResponse>(Json);
        Assert.NotNull(body);
        Assert.True(body!.Authenticated);

        var me = await client.GetAsync("/api/users/self");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var self = await me.Content.ReadFromJsonAsync<PrivateUserResponse>(Json);
        Assert.NotNull(self);
        Assert.Equal("invitee@example.com", self!.Email);
    }

    [Fact]
    public async Task Register_WithoutInvite_DoesNotAuthenticate()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register", new
        {
            email = $"plain-{Guid.NewGuid():N}@example.com",
            username = $"plain-{Guid.NewGuid():N}",
            fullName = "Plain User",
            password = "supersecure123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegistrationResponse>(Json);
        Assert.NotNull(body);
        Assert.False(body!.Authenticated);

        var me = await client.GetAsync("/api/users/self");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    private record RegistrationInfo(bool allow_registration);
    private record RegistrationResponse(string Detail, bool Authenticated);
    private record PrivateUserResponse(string Email);
}
