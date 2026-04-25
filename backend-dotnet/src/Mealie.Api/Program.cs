using Mealie.Api.Middleware;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──────────────────────────────────────────────────────────
builder.Configuration
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);
appSettings.Secret = Environment.GetEnvironmentVariable("SECRET") ?? appSettings.Secret;
appSettings.DatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? appSettings.DatabaseUrl;
appSettings.DbEngine = Environment.GetEnvironmentVariable("DB_ENGINE") ?? appSettings.DbEngine;
appSettings.DataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? appSettings.DataDir;
appSettings.LogLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? appSettings.LogLevel;
appSettings.OpenAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

builder.Services.AddSingleton(appSettings);
builder.Services.AddOptions<AppSettings>().Configure(o =>
{
    o.Secret = appSettings.Secret;
    o.DatabaseUrl = appSettings.DatabaseUrl;
    o.DbEngine = appSettings.DbEngine;
    o.DataDir = appSettings.DataDir;
    o.LogLevel = appSettings.LogLevel;
    o.OpenAiApiKey = appSettings.OpenAiApiKey;
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
});

// ── Serilog ────────────────────────────────────────────────────────────────
var logLevelEnum = Enum.TryParse<LogEventLevel>(appSettings.LogLevel, ignoreCase: true, out var lvl)
    ? lvl : LogEventLevel.Information;

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.MinimumLevel.Is(logLevelEnum)
       .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
       .MinimumLevel.Override("System", LogEventLevel.Warning)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .Enrich.WithThreadId();

    if (ctx.HostingEnvironment.IsProduction())
        cfg.WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter());
    else
        cfg.WriteTo.Console();
});

// ── Database ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<TenantFilter>();
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    if (appSettings.DbEngine.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        options.UseNpgsql(appSettings.DatabaseUrl)
               .UseSnakeCaseNamingConvention();
    else
        options.UseSqlite(appSettings.DatabaseUrl)
               .UseSnakeCaseNamingConvention();
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

builder.Services.AddAuthorization();

// ── Infrastructure Services ────────────────────────────────────────────────
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ── FluentValidation ───────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(typeof(Mealie.Application.PlaceholderMarker).Assembly);
builder.Services.AddFluentValidationAutoValidation();

// ── Controllers & JSON ────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// ── Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mealie API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
    c.CustomSchemaIds(type => type.Name.Replace("Dto", "").Replace("Request", "Request").Replace("Response", "Response"));
    c.OperationFilter<Mealie.Api.Filters.PydanticValidationOperationFilter>();
});

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
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(dataDir),
        RequestPath = ""
    });
}

app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz");

app.Run();

public partial class Program { }

