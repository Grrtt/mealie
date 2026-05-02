using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiConfigurationsAndSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS to be idempotent against existing dev databases
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ai_configurations (
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

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS site_settings (
                    id TEXT NOT NULL CONSTRAINT pk_site_settings PRIMARY KEY,
                    default_parser TEXT NOT NULL DEFAULT 'nlp',
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_ai_configurations_is_active
                ON ai_configurations (is_active);
                """);

            // Seed exactly one site_settings row with the default parser if none exists
            migrationBuilder.Sql("""
                INSERT INTO site_settings (id, default_parser, created_at, updated_at)
                SELECT lower(hex(randomblob(4))) || '-' ||
                       lower(hex(randomblob(2))) || '-' || '4' ||
                       substr(lower(hex(randomblob(2))), 2) || '-' ||
                       substr('89ab', abs(random()) % 4 + 1, 1) ||
                       substr(lower(hex(randomblob(2))), 2) || '-' ||
                       lower(hex(randomblob(6))),
                       'nlp', datetime('now'), datetime('now')
                WHERE NOT EXISTS (SELECT 1 FROM site_settings);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS ai_configurations;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS site_settings;");
        }
    }
}
