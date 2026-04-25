using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mealie.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Read DATABASE_URL from env for design-time use (migrations)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Data Source=./data/mealie.db";
        var dbEngine = Environment.GetEnvironmentVariable("DB_ENGINE") ?? "sqlite";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>();

        if (dbEngine.Equals("postgres", StringComparison.OrdinalIgnoreCase) ||
            dbEngine.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
        {
            options.UseNpgsql(databaseUrl);
        }
        else
        {
            options.UseSqlite(databaseUrl);
        }

        // Pass a no-op TenantFilter for design-time
        var tenantFilter = new TenantFilter();
        return new ApplicationDbContext(options.Options, tenantFilter);
    }
}
