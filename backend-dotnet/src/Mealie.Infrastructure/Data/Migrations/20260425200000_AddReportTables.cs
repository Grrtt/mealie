using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    category = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "migration"),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "in-progress"),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_reports_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    report_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    success = table.Column<bool>(type: "INTEGER", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: false),
                    exception = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_entries_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reports_group_id",
                table: "reports",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_entries_report_id",
                table: "report_entries",
                column: "report_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "report_entries");
            migrationBuilder.DropTable(name: "reports");
        }
    }
}
