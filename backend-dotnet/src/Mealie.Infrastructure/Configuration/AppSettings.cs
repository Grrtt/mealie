namespace Mealie.Infrastructure.Configuration;

public class AppSettings
{
    public string Secret { get; set; } = "changeme-at-least-32-characters-long-secret";
    public string DatabaseUrl { get; set; } = "Data Source=mealie.db";
    public string DbEngine { get; set; } = "sqlite";
    public string BaseUrl { get; set; } = "http://localhost:9000";
    public string DataDir { get; set; } = "/app/data";
    public string LogLevel { get; set; } = "Information";
    public int ApiPort { get; set; } = 9000;
    public bool AllowSignup { get; set; } = true;
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SmtpFromEmail { get; set; }
    public bool SmtpFromName_Mealie { get; set; } = true;
    public bool LdapEnabled { get; set; }
    public string? LdapServer { get; set; }
    public int LdapPort { get; set; } = 389;
    public int LdapQueryTimeout { get; set; } = 5;
    public string? LdapBindTemplate { get; set; }
    public string? LdapBaseDn { get; set; }
    public bool OidcEnabled { get; set; }
    public string? OidcClientId { get; set; }
    public string? OidcClientSecret { get; set; }
    public string? OidcAuthority { get; set; }
    public string? OpenAiApiKey { get; set; }
}
