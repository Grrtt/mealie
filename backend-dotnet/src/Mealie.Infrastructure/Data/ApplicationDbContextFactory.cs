using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mealie.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Read DATABASE_URL from env for design-time use (migrations)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                          ?? "Server=localhost,14330;Database=mealie;User Id=sa;Password=MealieSqlServerDev123!;TrustServerCertificate=True;Encrypt=False";
        var dbEngine = Environment.GetEnvironmentVariable("DB_ENGINE") ?? "sqlserver";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>();

        if (dbEngine.Equals("sqlserver", StringComparison.OrdinalIgnoreCase) ||
            dbEngine.Equals("mssql", StringComparison.OrdinalIgnoreCase))
        {
            options.UseSqlServer(databaseUrl).UseSnakeCaseNamingConvention();
        }
        else if (dbEngine.Equals("postgres", StringComparison.OrdinalIgnoreCase) ||
                 dbEngine.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
        {
            options.UseNpgsql(databaseUrl).UseSnakeCaseNamingConvention();
        }
        else
        {
            options.UseSqlite(databaseUrl).UseSnakeCaseNamingConvention();
        }

        // Pass a no-op TenantFilter for design-time
        var tenantFilter = new TenantFilter();
        return new ApplicationDbContext(options.Options, tenantFilter);
    }
}
