namespace Mealie.Api.Configuration;

/// <summary>
///     Documents all supported environment variables.
///     Actual binding happens in Program.cs via IConfiguration.
/// </summary>
public static class AppSettingsConfig
{
    // DB_ENGINE: "sqlite" (default) or "postgres"
    // DATABASE_URL: SQLite path or PostgreSQL connection string
    // SECRET: JWT signing key (min 32 chars)
    // BASE_URL: public-facing URL of the API
    // DATA_DIR: directory for recipe images, backups, etc.
    // LOG_LEVEL: Serilog level (default: Information)
    // API_PORT: listening port (default: 9000)
    // ALLOW_SIGNUP: true/false
    // LDAP_ENABLED: true/false
    // LDAP_SERVER, LDAP_PORT, LDAP_QUERY_TIMEOUT, LDAP_BIND_TEMPLATE, LDAP_BASE_DN
    // OIDC_ENABLED, OIDC_CLIENT_ID, OIDC_CLIENT_SECRET, OIDC_AUTHORITY
    // SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASSWORD, SMTP_FROM_EMAIL
}
