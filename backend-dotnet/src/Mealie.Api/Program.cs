using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mealie.Api.Caching;
using Mealie.Api.Commands;
using Mealie.Api.Filters;
using Mealie.Api.Mcp;
using Mealie.Api.Middleware;
using Mealie.Application;
using Mealie.Application.Contracts.Search;
using Mealie.Application.Queries;
using Mealie.Application.Services.Admin;
using Mealie.Application.Services.Auth;
using Mealie.Application.Services.Cookbooks;
using Mealie.Application.Services.Groups;
using Mealie.Application.Services.Households;
using Mealie.Application.Services.ImageScrape;
using Mealie.Application.Services.Ingredients;
using Mealie.Application.Services.MealPlans;
using Mealie.Application.Services.Migrations;
using Mealie.Application.Services.Organizers;
using Mealie.Application.Services.Parser;
using Mealie.Application.Services.Recipes;
using Mealie.Application.Services.Search;
using Mealie.Application.Services.Seeder;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Services.Users;
using Mealie.Application.Services.Webhooks;
using Mealie.Infrastructure.Admin;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Email;
using Mealie.Infrastructure.Parser;
using Mealie.Infrastructure.Scheduler;
using Mealie.Infrastructure.Scheduler.Jobs;
using Mealie.Infrastructure.Scraper;
using Mealie.Infrastructure.Scraper.Importers;
using Mealie.Infrastructure.Webhooks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──────────────────────────────────────────────────────────
builder.Configuration
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true);

var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);
appSettings.Secret = Environment.GetEnvironmentVariable("SECRET") ?? appSettings.Secret;
appSettings.DatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? appSettings.DatabaseUrl;
appSettings.DbEngine = Environment.GetEnvironmentVariable("DB_ENGINE") ?? appSettings.DbEngine;
appSettings.DataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? appSettings.DataDir;
appSettings.LogLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? appSettings.LogLevel;
appSettings.McpSecret = Environment.GetEnvironmentVariable("MCP_SECRET") ?? appSettings.McpSecret;

var allowSignupEnv = Environment.GetEnvironmentVariable("ALLOW_SIGNUP");
if (allowSignupEnv is not null)
{
    appSettings.AllowSignup = allowSignupEnv.Equals("true", StringComparison.OrdinalIgnoreCase);
}

builder.Services.AddSingleton(appSettings);
builder.Services.AddOptions<AppSettings>().Configure(o =>
{
    o.Secret = appSettings.Secret;
    o.DatabaseUrl = appSettings.DatabaseUrl;
    o.DbEngine = appSettings.DbEngine;
    o.DataDir = appSettings.DataDir;
    o.LogLevel = appSettings.LogLevel;
    o.AllowSignup = appSettings.AllowSignup;
    o.SmtpHost = appSettings.SmtpHost;
    o.SmtpPort = appSettings.SmtpPort;
    o.SmtpUser = appSettings.SmtpUser;
    o.SmtpPassword = appSettings.SmtpPassword;
    o.SmtpFromEmail = appSettings.SmtpFromEmail;
    o.LdapEnabled = appSettings.LdapEnabled;
    o.LdapServer = appSettings.LdapServer;
    o.LdapPort = appSettings.LdapPort;
    o.LdapQueryTimeout = appSettings.LdapQueryTimeout;
    o.OidcEnabled = appSettings.OidcEnabled;
    o.OidcClientId = appSettings.OidcClientId;
    o.OidcClientSecret = appSettings.OidcClientSecret;
    o.OidcAuthority = appSettings.OidcAuthority;
    o.ApiPort = appSettings.ApiPort;
    o.BaseUrl = appSettings.BaseUrl;
    o.McpSecret = appSettings.McpSecret;
});

// ── MCP Log Store ──────────────────────────────────────────────────────────
var inMemoryLogStore = new InMemoryLogStore();
builder.Services.AddSingleton(inMemoryLogStore);

// ── Serilog ────────────────────────────────────────────────────────────────
var logLevelEnum = Enum.TryParse<LogEventLevel>(appSettings.LogLevel, true, out var lvl)
    ? lvl
    : LogEventLevel.Information;

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.MinimumLevel.Is(logLevelEnum)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Sink(inMemoryLogStore);

    if (ctx.HostingEnvironment.IsProduction())
    {
        cfg.WriteTo.Console(new CompactJsonFormatter());
    }
    else
    {
        cfg.WriteTo.Console();
    }
});

// ── Database ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<TenantFilter>();
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    if (appSettings.DbEngine.Equals("sqlserver", StringComparison.OrdinalIgnoreCase) ||
        appSettings.DbEngine.Equals("mssql", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(appSettings.DatabaseUrl)
            .UseSnakeCaseNamingConvention();
    }
    else if (appSettings.DbEngine.Equals("postgres", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(appSettings.DatabaseUrl)
            .UseSnakeCaseNamingConvention();
    }
    else
    {
        options.UseSqlite(appSettings.DatabaseUrl)
            .UseSnakeCaseNamingConvention();
    }

    // Suppress warning when a hand-written migration's snapshot doesn't exactly
    // match EF's internal representation — migrations themselves are correct.
    options.ConfigureWarnings(w =>
        w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// ── Tenant Context ─────────────────────────────────────────────────────────
builder.Services.AddScoped<ITenantContext, TenantContextAccessor>();

// ── Auth ───────────────────────────────────────────────────────────────────
var key = Encoding.UTF8.GetBytes(appSettings.Secret);
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = "mealie",
            ValidateAudience = true,
            ValidAudience = "mealie",
            ClockSkew = TimeSpan.Zero
        };
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// ── Infrastructure Services ────────────────────────────────────────────────
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Auth services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<ILdapAuthService, LdapAuthService>();
builder.Services.AddScoped<IOidcService, OidcService>();
builder.Services.AddHttpClient("oidc", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Application services — Recipes
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IRecipeCommentService, RecipeCommentService>();
builder.Services.AddScoped<IRecipeTimelineService, RecipeTimelineService>();
builder.Services.AddScoped<IRecipeAssetService, RecipeAssetService>();
builder.Services.AddScoped<IRecipeShareService, RecipeShareService>();
builder.Services.AddScoped<IRecipeExportService, RecipeExportService>();
builder.Services.AddScoped<IRecipeImportService, RecipeImportService>();
builder.Services.AddScoped<IRecipeScraperService, RecipeScraperService>();
builder.Services.AddHttpClient<RecipeScraperService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
});

// Query pattern
builder.Services.AddScoped<IQueryServices, QueryServices>();
builder.Services.AddScoped<QueryExecutor>();

// Application services — Admin
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminGroupService, AdminGroupService>();

// Application services — Users
builder.Services.AddScoped<IUserService, UserService>();

// Application services — Groups & Households
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IGroupLabelService, GroupLabelService>();
builder.Services.AddScoped<IHouseholdService, HouseholdService>();

// Application services — Seeder
builder.Services.AddScoped<ISeederService, SeederService>();

// Application services — Organizers
builder.Services.AddScoped<IOrganizerService, OrganizerService>();
builder.Services.AddScoped<ICookbookService, CookbookService>();

// Application services — Ingredients
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IUnitService, UnitService>();

// Application services — Meal Plans & Shopping
builder.Services.AddScoped<IMealPlanService, MealPlanService>();
builder.Services.AddScoped<IMealPlanRuleService, MealPlanRuleService>();
builder.Services.AddScoped<IShoppingListService, ShoppingListService>();

// Phase 7: Background services
builder.Services.AddHttpClient<IWebhookDeliveryService, WebhookDeliveryService>(c =>
    c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHttpClient<AppriseNotificationHandler>(c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHttpClient("OpenAi", (_, c) =>
{
    c.BaseAddress = new Uri("https://api.openai.com/v1/");
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IEventNotifierService, EventNotifierService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IApiKeyEncryptionService>(sp =>
    new ApiKeyEncryptionService(appSettings.Secret, sp.GetRequiredService<ILogger<ApiKeyEncryptionService>>()));
builder.Services.AddScoped<ScheduledBackupJob>();
builder.Services.AddScoped<MealPlanNotificationJob>();
builder.Services.AddHostedService<SchedulerHostedService>();

// Phase 8: Ingredient parser
builder.Services.AddScoped<UnitMatcher>();
builder.Services.AddScoped<FoodMatcher>();
builder.Services.AddScoped<BruteParserStrategy>();
builder.Services.AddScoped<NlpParserStrategy>();
builder.Services.AddScoped<IParserStrategyResolver, ParserStrategyResolver>();
builder.Services.AddScoped<IIngredientParserService, IngredientParserService>();
builder.Services.AddScoped<IRecipeOrganizerService, RecipeOrganizerService>();

// Phase 9: Migration importers
builder.Services.AddSingleton<IMigrationParser, ChowdownMigrationParser>();
builder.Services.AddSingleton<IMigrationParser, PaprikaMigrationParser>();
builder.Services.AddSingleton<IMigrationParser, NextcloudCookbookMigrationParser>();
builder.Services.AddSingleton<IMigrationParser, TandoorMigrationParser>();
builder.Services.AddSingleton<IMigrationParser, MealieBackupImportParser>();
builder.Services.AddScoped<MigrationImportService>();

// Migration queue: singleton channel + hosted background processor
builder.Services.AddSingleton<MigrationQueue>();
builder.Services.AddHostedService<MigrationBackgroundService>();

// Image scrape queue: singleton channel + hosted background processor
builder.Services.AddSingleton<ImageScrapeQueue>();
builder.Services.AddHostedService<ImageScrapeBackgroundService>();

// Seed queue: singleton channel + hosted background processor
builder.Services.AddSingleton<SeedQueue>();
builder.Services.AddHostedService<SeedBackgroundService>();

// MediatR — scan Application + Infrastructure + Api assemblies for handlers
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(RecipeSearchIndexHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(AppriseNotificationHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// Search indexes: singleton Lucene implementations + startup rebuild
builder.Services.AddSingleton<IRecipeSearchIndex, LuceneRecipeSearchIndex>();
builder.Services.AddSingleton<IFoodSearchIndex, LuceneFoodSearchIndex>();
builder.Services.AddSingleton<IIndexDiagnostics>(sp =>
    (IIndexDiagnostics)sp.GetRequiredService<IRecipeSearchIndex>());
builder.Services.AddSingleton<IIndexDiagnostics>(sp =>
    (IIndexDiagnostics)sp.GetRequiredService<IFoodSearchIndex>());
builder.Services.AddScoped<IIndexAdminService, IndexAdminService>();
builder.Services.AddHostedService<SearchIndexRebuildService>();

// Ingredient NLP parser: singleton Python subprocess bridge
builder.Services.AddSingleton<Mealie.Application.Services.IngredientParser.IngredientParserService>();

// ── MCP Server ─────────────────────────────────────────────────────────────
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<LogsTool>()
    .WithTools<RecipeSearchTool>()
    .WithTools<OrganizerSearchTool>();

// ── FluentValidation ───────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(typeof(PlaceholderMarker).Assembly);
builder.Services.AddFluentValidationAutoValidation();

// ── Controllers & JSON ────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; });

// ── Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mealie API",
        Version = "v1",
        Description = "Mealie Recipe Manager — C# Backend"
    });

    // Match Python Pydantic model names exactly (strip "Dto" suffix for OpenAPI compatibility)
    options.CustomSchemaIds(type =>
    {
        var name = type.Name;
        if (name.EndsWith("Dto"))
        {
            name = name[..^3];
        }

        return name;
    });

    // Add JWT bearer auth
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
                { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });

    options.OperationFilter<PydanticValidationOperationFilter>();

    // Add XML comments if file exists
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ── Output Cache ───────────────────────────────────────────────────────────
builder.Services.AddOutputCache(options =>
    options.AddPolicy(RecipeListCachePolicy.Name, RecipeListCachePolicy.Instance));

// ── Health Checks ──────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Build App ──────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────────
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "anonymous");
        diagnosticContext.Set("HouseholdId", httpContext.User.FindFirst("household_id")?.Value ?? "");
        diagnosticContext.Set("GroupId", httpContext.User.FindFirst("group_id")?.Value ?? "");
    };
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();
app.UseOutputCache();
app.UseMiddleware<ValidationExceptionMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => c.RouteTemplate = "api/openapi.json");
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/api/openapi.json", "Mealie API v1");
        c.RoutePrefix = "api/docs";
    });
}
else
{
    app.UseSwagger(c => c.RouteTemplate = "api/openapi.json");
}

var dataDir = appSettings.DataDir;
if (Directory.Exists(dataDir))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(dataDir),
        RequestPath = ""
    });
}

app.MapControllers();

// Simple Bearer-token guard for the MCP endpoint.
app.MapMcp("/mcp")
    .AddEndpointFilter(async (ctx, next) =>
    {
        var secret = appSettings.McpSecret;
        if (string.IsNullOrEmpty(secret))
        {
            ctx.HttpContext.Response.StatusCode = 401;
            return Results.Unauthorized();
        }
        var auth = ctx.HttpContext.Request.Headers.Authorization.ToString();
        var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? auth["Bearer ".Length..].Trim()
            : null;
        if (token != secret)
        {
            ctx.HttpContext.Response.StatusCode = 401;
            return Results.Unauthorized();
        }
        return await next(ctx);
    });
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz");

// Handle CLI verbs
if (args.Length > 0)
{
    switch (args[0].ToLower())
    {
        case "seed":
            await SeedCommand.RunAsync(app.Services);
            return;
        case "migrate":
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await db.Database.EnsureCreatedAsync();
                    Console.WriteLine("✅ SQL Server bootstrap applied from current EF model.");
                }
                else
                {
                    await db.Database.MigrateAsync();
                    Console.WriteLine("✅ Migrations applied.");
                }
            }

            return;
    }
}

// Apply migrations and seed on every startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var isSqlServer = db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
    if (isSqlServer)
    {
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();

        // Ensure report tables exist — applied as raw SQL so it's always idempotent
        // regardless of EF migration discovery issues with hand-written migrations.
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS reports (
                id TEXT NOT NULL,
                name TEXT NOT NULL,
                category TEXT NOT NULL DEFAULT 'migration',
                status TEXT NOT NULL DEFAULT 'in-progress',
                timestamp TEXT NOT NULL,
                group_id TEXT NOT NULL,
                CONSTRAINT pk_reports PRIMARY KEY (id),
                CONSTRAINT fk_reports_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_reports_group_id ON reports (group_id);
            CREATE TABLE IF NOT EXISTS report_entries (
                id TEXT NOT NULL,
                report_id TEXT NOT NULL,
                timestamp TEXT NOT NULL,
                success INTEGER NOT NULL,
                message TEXT NOT NULL,
                exception TEXT,
                CONSTRAINT pk_report_entries PRIMARY KEY (id),
                CONSTRAINT fk_report_entries_reports_report_id FOREIGN KEY (report_id) REFERENCES reports (id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_report_entries_report_id ON report_entries (report_id);
        ");

        // Ensure site_settings system-prompt columns exist — idempotent guard for hand-written migration discovery issues.
        var promptCols = new[] { "ingredient_system_prompt", "category_system_prompt", "tag_system_prompt" };
        var existingCols = new HashSet<string>();
        var isPostgres = db.Database.ProviderName?.Contains("Npgsql") == true;
        await using (var conn = db.Database.GetDbConnection())
        {
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = isPostgres
                ? """
                    SELECT column_name
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'site_settings'
                    """
                : "SELECT name FROM pragma_table_info('site_settings')";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                existingCols.Add(reader.GetString(0));
            if (!wasOpen) conn.Close();
        }

        foreach (var col in promptCols.Where(c => !existingCols.Contains(c)))
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE site_settings ADD COLUMN " + col + " TEXT");
    }
}

await SeedCommand.RunAsync(app.Services);


app.Run();

public partial class Program
{
}
