using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;

namespace Mealie.Api.Commands;

public static class SeedCommand
{
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
            FullName = "Admin",
            Username = "admin",
            Email = "admin@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("admin"),
            Admin = true,
            Advanced = true,
            GroupId = groupId,
            HouseholdId = householdId,
            CanManage = true,
            CanManageHousehold = true,
            CanInvite = true,
            CanOrganize = true,
            AuthMethod = Domain.Entities.Core.AuthMethod.Mealie,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Users.Add(admin);

        // Sample recipes
        var sampleRecipes = new[]
        {
            ("Spaghetti Carbonara", "spaghetti-carbonara", "Classic Italian pasta dish"),
            ("Chicken Tikka Masala", "chicken-tikka-masala", "Popular British-Indian curry"),
            ("Avocado Toast", "avocado-toast", "Simple and healthy breakfast"),
            ("Beef Tacos", "beef-tacos", "Quick and easy weeknight dinner"),
            ("Caesar Salad", "caesar-salad", "Classic Caesar salad with homemade dressing"),
        };

        foreach (var (name, slug, description) in sampleRecipes)
        {
            db.Recipes.Add(new Recipe
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = slug,
                Description = description,
                GroupId = groupId,
                HouseholdId = householdId,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("✅ Seed completed. Admin: admin@example.com / admin");
        Console.WriteLine("✅ Database seeded successfully!");
        Console.WriteLine("   Admin user: admin@example.com / admin");
        Console.WriteLine("   API: http://localhost:9000");
    }
}
