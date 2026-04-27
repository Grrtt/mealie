using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportQueueFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "queued_file_path",
                table: "reports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "queued_household_id",
                table: "reports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "queued_user_id",
                table: "reports",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "queued_file_path", table: "reports");
            migrationBuilder.DropColumn(name: "queued_household_id", table: "reports");
            migrationBuilder.DropColumn(name: "queued_user_id", table: "reports");
        }
    }
}
