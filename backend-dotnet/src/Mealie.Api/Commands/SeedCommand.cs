using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Recipes;
using Mealie.Domain.Entities.Settings;
using Mealie.Infrastructure.Data;

namespace Mealie.Api.Commands;

public static class SeedCommand
{
    public const string DefaultEmail = "changeme@example.com";
    public const string DefaultPassword = "MyPassword";

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Check if already seeded
        if (db.Groups.Any())
        {
            logger.LogInformation("Database already has data — skipping seed.");
            return;
        }

        logger.LogInformation("Seeding database with default data...");

        var groupId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            Name = "Home",
            Slug = "home",
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Groups.Add(group);

        var household = new Household
        {
            Id = householdId,
            Name = "Family",
            Slug = "family",
            GroupId = groupId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Households.Add(household);

        var admin = new User
        {
            Id = userId,
            FullName = "Change Me",
            Username = "changeme",
            Email = DefaultEmail,
            Password = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
            Admin = true,
            Advanced = true,
            GroupId = groupId,
            HouseholdId = householdId,
            CanManage = true,
            CanManageHousehold = true,
            CanInvite = true,
            CanOrganize = true,
            AuthMethod = AuthMethod.Mealie,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Users.Add(admin);

        db.GroupPreferences.Add(new GroupPreferences
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            PrivateGroup = false,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        });

        db.HouseholdPreferences.Add(new HouseholdPreferences
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            PrivateHousehold = false,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        logger.LogInformation("✅ Seed completed. Default admin: {Email} / {Password}", DefaultEmail, DefaultPassword);
    }
}
