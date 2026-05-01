using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ingredient_units_aliases",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    unit_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredient_units_aliases", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredient_units_aliases_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "ingredient_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_units_aliases_unit_id",
                table: "ingredient_units_aliases",
                column: "unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingredient_units_aliases");
        }
    }
}
