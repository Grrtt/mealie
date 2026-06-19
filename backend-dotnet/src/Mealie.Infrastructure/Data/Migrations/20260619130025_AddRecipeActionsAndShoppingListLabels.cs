using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeActionsAndShoppingListLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_plan_rules_to_categories_categories_category_id",
                table: "plan_rules_to_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_plan_rules_to_households_households_household_id",
                table: "plan_rules_to_households");

            migrationBuilder.DropForeignKey(
                name: "fk_plan_rules_to_tags_tags_tag_id",
                table: "plan_rules_to_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_categories_categories_category_id",
                table: "recipes_to_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tags_tags_tag_id",
                table: "recipes_to_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tools_tools_tool_id",
                table: "recipes_to_tools");

            migrationBuilder.AlterColumn<int>(
                name: "is_active",
                table: "ai_configurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "enable_transcription_services",
                table: "ai_configurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "enable_image_services",
                table: "ai_configurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.CreateTable(
                name: "recipe_actions",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    url = table.Column<string>(type: "TEXT", nullable: false),
                    action_type = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    group_id = table.Column<string>(type: "TEXT", nullable: false),
                    household_id = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false),
                    update_at = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_actions_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipe_actions_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "ix_recipe_actions_group_id",
                table: "recipe_actions",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_actions_household_id",
                table: "recipe_actions",
                column: "household_id");

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

            migrationBuilder.AddForeignKey(
                name: "fk_plan_rules_to_categories_categories_category_id",
                table: "plan_rules_to_categories",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_plan_rules_to_households_households_household_id",
                table: "plan_rules_to_households",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_plan_rules_to_tags_tags_tag_id",
                table: "plan_rules_to_tags",
                column: "tag_id",
                principalTable: "tags",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_categories_categories_category_id",
                table: "recipes_to_categories",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_tags_tags_tag_id",
                table: "recipes_to_tags",
                column: "tag_id",
                principalTable: "tags",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_tools_tools_tool_id",
                table: "recipes_to_tools",
                column: "tool_id",
                principalTable: "tools",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_plan_rules_to_categories_categories_category_id",
                table: "plan_rules_to_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_plan_rules_to_households_households_household_id",
                table: "plan_rules_to_households");

            migrationBuilder.DropForeignKey(
                name: "fk_plan_rules_to_tags_tags_tag_id",
                table: "plan_rules_to_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_categories_categories_category_id",
                table: "recipes_to_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tags_tags_tag_id",
                table: "recipes_to_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tools_tools_tool_id",
                table: "recipes_to_tools");

            migrationBuilder.DropTable(
                name: "recipe_actions");

            migrationBuilder.DropTable(
                name: "shopping_list_labels");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "ai_configurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "enable_transcription_services",
                table: "ai_configurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<bool>(
                name: "enable_image_services",
                table: "ai_configurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 1);

            migrationBuilder.AddForeignKey(
                name: "fk_plan_rules_to_categories_categories_category_id",
                table: "plan_rules_to_categories",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_plan_rules_to_households_households_household_id",
                table: "plan_rules_to_households",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_plan_rules_to_tags_tags_tag_id",
                table: "plan_rules_to_tags",
                column: "tag_id",
                principalTable: "tags",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_categories_categories_category_id",
                table: "recipes_to_categories",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_tags_tags_tag_id",
                table: "recipes_to_tags",
                column: "tag_id",
                principalTable: "tags",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_tools_tools_tool_id",
                table: "recipes_to_tools",
                column: "tool_id",
                principalTable: "tools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
