using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportProgressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite supports ALTER TABLE ADD COLUMN directly for nullable columns
            // and columns with defaults, avoiding a full table rebuild that would
            // conflict with the existing snake_case table name.
            migrationBuilder.Sql("ALTER TABLE \"reports\" ADD COLUMN \"total_count\" INTEGER NULL;");
            migrationBuilder.Sql("ALTER TABLE \"reports\" ADD COLUMN \"processed_count\" INTEGER NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // SQLite does not support DROP COLUMN in all versions; no-op is safe here
            // since the columns are additive and harmless if present.
        }
    }
}
