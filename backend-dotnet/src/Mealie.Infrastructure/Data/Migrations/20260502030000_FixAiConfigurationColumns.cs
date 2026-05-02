using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixAiConfigurationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safely ensures ai_configurations has all required columns using the SQLite
            // 12-step table recreation approach. Works on:
            //   - Fresh DBs (migration 1 created the table correctly; this is a no-op copy)
            //   - Broken dev DBs where the table existed before enable_image_services/
            //     enable_transcription_services columns were added
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ai_configurations_v2 (
                    id TEXT NOT NULL CONSTRAINT pk_ai_configurations PRIMARY KEY,
                    name TEXT NOT NULL,
                    provider_type TEXT NOT NULL,
                    encrypted_api_key TEXT,
                    base_url TEXT,
                    default_model TEXT,
                    is_active INTEGER NOT NULL DEFAULT 0,
                    enable_image_services INTEGER NOT NULL DEFAULT 1,
                    enable_transcription_services INTEGER NOT NULL DEFAULT 1,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                """);

            // Copy only the columns guaranteed to exist in all DB states; new columns get defaults.
            migrationBuilder.Sql("""
                INSERT INTO ai_configurations_v2
                    (id, name, provider_type, encrypted_api_key, base_url, default_model,
                     is_active, created_at, updated_at)
                SELECT id, name, provider_type, encrypted_api_key, base_url, default_model,
                       is_active, created_at, updated_at
                FROM ai_configurations;
                """);

            migrationBuilder.Sql("DROP TABLE ai_configurations;");
            migrationBuilder.Sql("ALTER TABLE ai_configurations_v2 RENAME TO ai_configurations;");

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_ai_configurations_is_active
                ON ai_configurations (is_active);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down: recreate without the two feature-flag columns
            migrationBuilder.Sql("""
                CREATE TABLE ai_configurations_v1 (
                    id TEXT NOT NULL CONSTRAINT pk_ai_configurations PRIMARY KEY,
                    name TEXT NOT NULL,
                    provider_type TEXT NOT NULL,
                    encrypted_api_key TEXT,
                    base_url TEXT,
                    default_model TEXT,
                    is_active INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO ai_configurations_v1
                    (id, name, provider_type, encrypted_api_key, base_url, default_model,
                     is_active, created_at, updated_at)
                SELECT id, name, provider_type, encrypted_api_key, base_url, default_model,
                       is_active, created_at, updated_at
                FROM ai_configurations;
                """);

            migrationBuilder.Sql("DROP TABLE ai_configurations;");
            migrationBuilder.Sql("ALTER TABLE ai_configurations_v1 RENAME TO ai_configurations;");
        }
    }
}
