using System.Net;
using System.Net.Http.Headers;
using Mealie.Api.Commands;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Recipes;
using Mealie.Domain.Entities.Settings;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Mealie.IntegrationTests.Recipes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mealie.IntegrationTests.Media;

public class MediaTestFactory : RecipeDeleteTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        // The old static-file mapping required DATA_DIR to exist at startup;
        // keep creating it so the static-path bypass tests exercise a realistic layout.
        Directory.CreateDirectory(Environment.GetEnvironmentVariable("DATA_DIR")!);
    }
}

public class MediaAuthorizationIntegrationTests(MediaTestFactory factory) : IClassFixture<MediaTestFactory>
{
    [Theory]
    [InlineData(false, false, "anonymous", HttpStatusCode.NotFound)]
    [InlineData(true, false, "anonymous", HttpStatusCode.OK)]
    [InlineData(true, true, "anonymous", HttpStatusCode.NotFound)]
    [InlineData(false, true, "owner", HttpStatusCode.OK)]
    [InlineData(false, false, "householdMember", HttpStatusCode.OK)]
    [InlineData(false, false, "household", HttpStatusCode.NotFound)]
    [InlineData(false, false, "group", HttpStatusCode.NotFound)]
    [InlineData(true, false, "household", HttpStatusCode.OK)]
    [InlineData(true, false, "group", HttpStatusCode.OK)]
    [InlineData(true, true, "household", HttpStatusCode.NotFound)]
    [InlineData(true, true, "group", HttpStatusCode.NotFound)]
    public async Task RecipeImages_EnforceVisibility(
        bool isPublic, bool privateHousehold, string caller, HttpStatusCode expected)
    {
        var media = await SeedMedia(isPublic, privateHousehold);
        using var client = CreateClient(media, caller);

        var response = await client.GetAsync($"/api/media/recipes/{media.RecipeId}/images/original.webp");

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            Assert.Equal("image/webp", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("private media", await response.Content.ReadAsStringAsync());
            Assert.True(response.Headers.CacheControl?.NoStore);
        }
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("owner", HttpStatusCode.OK)]
    [InlineData("householdMember", HttpStatusCode.OK)]
    [InlineData("household", HttpStatusCode.NotFound)]
    [InlineData("group", HttpStatusCode.NotFound)]
    public async Task RestrictedMedia_RequiresHouseholdAccess(string caller, HttpStatusCode expected)
    {
        // Assets, timeline images, and profile images never leak to anonymous
        // callers or to other households/groups — even for public recipes.
        var media = await SeedMedia(isPublic: true);
        using var client = CreateClient(media, caller);
        var paths = new[]
        {
            $"/api/media/recipes/{media.RecipeId}/assets/attachment.pdf",
            $"/api/media/recipes/{media.RecipeId}/images/timeline/{media.EventId}/original.webp",
            $"/api/media/users/{media.UserId}/profile.webp"
        };

        foreach (var path in paths)
        {
            var response = await client.GetAsync(path);
            Assert.Equal(expected, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("owner")]
    public async Task DataDirectory_CannotBeReadThroughStaticPaths(string caller)
    {
        // The API used to mount DATA_DIR as unauthenticated static files. Real
        // files exist on disk and DATA_DIR existed at startup, but no static
        // route may serve them anymore.
        var media = await SeedMedia();
        using var client = CreateClient(media, caller);
        var paths = new[]
        {
            $"/recipes/{media.RecipeId}/images/original.webp",
            $"/recipes/{media.RecipeId}/assets/attachment.pdf",
            $"/recipes/{media.RecipeId}/images/timeline/{media.EventId}/original.webp",
            $"/users/{media.UserId}/profile.webp",
            "/backups/private.zip"
        };

        foreach (var path in paths)
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task MissingRecipesAndFiles_ReturnNotFound()
    {
        var media = await SeedMedia(isPublic: true);
        using var client = CreateClient(media, "owner");
        var paths = new[]
        {
            $"/api/media/recipes/{Guid.NewGuid()}/images/original.webp",
            $"/api/media/recipes/{media.RecipeId}/images/missing.webp",
            $"/api/media/recipes/{media.RecipeId}/assets/missing.pdf"
        };

        foreach (var path in paths)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        }
    }

    [Fact]
    public async Task CookieAuthenticatedClients_CanLoadPrivateImages()
    {
        // <img> tags send the session cookie, not an Authorization header, so a
        // plain login round-trip must be enough to view private media.
        var media = await SeedCookieMedia();
        using var client = factory.CreateClient();

        var login = await client.PostAsync("/api/auth/token", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", SeedCommand.DefaultEmail),
            new KeyValuePair<string, string>("password", SeedCommand.DefaultPassword)
        }));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var image = await client.GetAsync($"/api/media/recipes/{media.RecipeId}/images/original.webp");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("cookie media", await image.Content.ReadAsStringAsync());

        var profile = await client.GetAsync($"/api/media/users/{media.UserId}/profile.webp");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.Equal("cookie media", await profile.Content.ReadAsStringAsync());

        // Without the cookie, the same private images stay hidden.
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound,
            (await anonymous.GetAsync($"/api/media/recipes/{media.RecipeId}/images/original.webp")).StatusCode);
    }

    [Fact]
    public async Task TraversalAndRootedNames_AreRejected()
    {
        var media = await SeedMedia(isPublic: true);
        using var client = CreateClient(media, "owner");
        var paths = new (string Path, HttpStatusCode Expected)[]
        {
            // Encoded slashes are decoded before routing, so traversal names
            // containing '/' can never reach a media route.
            ($"/api/media/recipes/{media.RecipeId}/images/..%2F..%2Fusers%2F{media.UserId}%2Fprofile.webp", HttpStatusCode.NotFound),
            ($"/api/media/users/{media.UserId}/..%2F..%2Frecipes%2F{media.RecipeId}%2Fimages%2Foriginal.webp", HttpStatusCode.NotFound),
            ($"/api/media/recipes/{media.RecipeId}/images/%2e%2e%2f%2e%2e%2fusers%2f{media.UserId}%2fprofile.webp", HttpStatusCode.NotFound),
            // Rooted absolute paths cannot become a single file name either.
            ($"/api/media/recipes/{media.RecipeId}/images/%2Fetc%2Fpasswd", HttpStatusCode.NotFound),
            // Encoded backslashes stay inside one segment; the controller rejects
            // names containing either slash type.
            ($"/api/media/recipes/{media.RecipeId}/images/..%5Cusers%5C{media.UserId}%5Cprofile.webp", HttpStatusCode.BadRequest),
            // Double-encoded separators are treated as literal file names and
            // simply do not exist on disk.
            ($"/api/media/recipes/{media.RecipeId}/images/..%252F..%252Fusers%252F{media.UserId}%252Fprofile.webp", HttpStatusCode.NotFound)
        };

        foreach (var (path, expected) in paths)
        {
            var response = await client.GetAsync(path);
            Assert.Equal(expected, response.StatusCode);
            Assert.NotEqual("private media", await response.Content.ReadAsStringAsync());
        }
    }

    private async Task<MediaData> SeedMedia(bool isPublic = false, bool privateHousehold = false)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == SeedCommand.DefaultEmail);
        var household = new Household
        {
            Id = Guid.NewGuid(), GroupId = user.GroupId, Name = "Media household",
            Slug = $"media-{Guid.NewGuid():N}",
            Preferences = new HouseholdPreferences { Id = Guid.NewGuid(), PrivateHousehold = privateHousehold }
        };
        var mediaUser = new User
        {
            Id = Guid.NewGuid(), GroupId = user.GroupId, HouseholdId = household.Id,
            Username = $"media-{Guid.NewGuid():N}"
        };
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(), Name = "Media recipe", Slug = $"media-{Guid.NewGuid():N}",
            GroupId = user.GroupId, HouseholdId = household.Id,
            Settings = new RecipeSettings { Public = isPublic }
        };
        db.Households.Add(household);
        db.Users.Add(mediaUser);
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var media = new MediaData(recipe.Id, Guid.NewGuid(), mediaUser.Id, user.GroupId, household.Id);
        await WriteMediaFiles("private media",
            $"recipes/{media.RecipeId}/images/original.webp",
            $"recipes/{media.RecipeId}/assets/attachment.pdf",
            $"recipes/{media.RecipeId}/images/timeline/{media.EventId}/original.webp",
            $"users/{media.UserId}/profile.webp",
            "backups/private.zip");
        return media;
    }

    private async Task<CookieMediaData> SeedCookieMedia()
    {
        // Media owned by the seeded default user — the only account we can log in
        // as — to prove cookie-based authentication works end to end.
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == SeedCommand.DefaultEmail);
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(), Name = "Cookie media recipe", Slug = $"cookie-{Guid.NewGuid():N}",
            GroupId = user.GroupId, HouseholdId = user.HouseholdId!.Value,
            Settings = new RecipeSettings { Public = false }
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        await WriteMediaFiles("cookie media",
            $"recipes/{recipe.Id}/images/original.webp",
            $"users/{user.Id}/profile.webp");
        return new CookieMediaData(recipe.Id, user.Id);
    }

    private async Task WriteMediaFiles(string content, params string[] relativePaths)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dataDir = scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>().Value.DataDir;
        foreach (var relativePath in relativePaths)
        {
            var path = Path.Combine(dataDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, content);
        }
    }

    private HttpClient CreateClient(MediaData media, string caller)
    {
        var client = factory.CreateClient();
        if (caller == "anonymous")
        {
            return client;
        }

        using var scope = factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var groupId = media.GroupId;
        var householdId = media.HouseholdId;
        if (caller == "group")
        {
            groupId = Guid.NewGuid();
        }

        if (caller == "household")
        {
            householdId = Guid.NewGuid();
        }

        // Only "owner" keeps the media user's ID; every other caller is a
        // distinct user, so household/group mismatches are actually exercised.
        var userId = caller == "owner" ? media.UserId : Guid.NewGuid();
        var token = tokens.GenerateAccessToken(userId, groupId, householdId, false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private record MediaData(Guid RecipeId, Guid EventId, Guid UserId, Guid GroupId, Guid HouseholdId);

    private record CookieMediaData(Guid RecipeId, Guid UserId);
}
