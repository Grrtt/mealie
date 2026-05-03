using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteSettingsSystemPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ingredient_system_prompt",
                table: "site_settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category_system_prompt",
                table: "site_settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tag_system_prompt",
                table: "site_settings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ingredient_system_prompt",
                table: "site_settings");

            migrationBuilder.DropColumn(
                name: "category_system_prompt",
                table: "site_settings");

            migrationBuilder.DropColumn(
                name: "tag_system_prompt",
                table: "site_settings");
        }
    }
}
