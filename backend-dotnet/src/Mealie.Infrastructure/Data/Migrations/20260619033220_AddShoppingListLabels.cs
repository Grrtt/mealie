using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingListLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shopping_list_labels",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    shopping_list_id = table.Column<string>(type: "TEXT", nullable: false),
                    label_id = table.Column<string>(type: "TEXT", nullable: false),
                    position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_list_labels", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopping_list_labels_labels_label_id",
                        column: x => x.label_id,
                        principalTable: "multi_purpose_labels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shopping_list_labels_shopping_lists_shopping_list_id",
                        column: x => x.shopping_list_id,
                        principalTable: "shopping_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_labels_label_id",
                table: "shopping_list_labels",
                column: "label_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_labels_shopping_list_id",
                table: "shopping_list_labels",
                column: "shopping_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_labels_shopping_list_id_position",
                table: "shopping_list_labels",
                columns: new[] { "shopping_list_id", "position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shopping_list_labels");
        }
    }
}
