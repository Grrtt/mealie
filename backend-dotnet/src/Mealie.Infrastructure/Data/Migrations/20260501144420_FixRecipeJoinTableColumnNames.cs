using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixRecipeJoinTableColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_categories_categories_categories_id",
                table: "recipes_to_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_categories_recipes_recipes_id",
                table: "recipes_to_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tags_recipes_recipes_id",
                table: "recipes_to_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tags_tags_tags_id",
                table: "recipes_to_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tools_recipes_recipes_id",
                table: "recipes_to_tools");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tools_tools_tools_id",
                table: "recipes_to_tools");

            migrationBuilder.RenameColumn(
                name: "tools_id",
                table: "recipes_to_tools",
                newName: "tool_id");

            migrationBuilder.RenameColumn(
                name: "recipes_id",
                table: "recipes_to_tools",
                newName: "recipe_id");

            migrationBuilder.RenameIndex(
                name: "ix_recipes_to_tools_tools_id",
                table: "recipes_to_tools",
                newName: "ix_recipes_to_tools_tool_id");

            migrationBuilder.RenameColumn(
                name: "tags_id",
                table: "recipes_to_tags",
                newName: "tag_id");

            migrationBuilder.RenameColumn(
                name: "recipes_id",
                table: "recipes_to_tags",
                newName: "recipe_id");

            migrationBuilder.RenameIndex(
                name: "ix_recipes_to_tags_tags_id",
                table: "recipes_to_tags",
                newName: "ix_recipes_to_tags_tag_id");

            migrationBuilder.RenameColumn(
                name: "recipes_id",
                table: "recipes_to_categories",
                newName: "recipe_id");

            migrationBuilder.RenameColumn(
                name: "categories_id",
                table: "recipes_to_categories",
                newName: "category_id");

            migrationBuilder.RenameIndex(
                name: "ix_recipes_to_categories_recipes_id",
                table: "recipes_to_categories",
                newName: "ix_recipes_to_categories_recipe_id");

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_categories_categories_category_id",
                table: "recipes_to_categories",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_categories_recipes_recipe_id",
                table: "recipes_to_categories",
                column: "recipe_id",
                principalTable: "recipes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_tags_recipes_recipe_id",
                table: "recipes_to_tags",
                column: "recipe_id",
                principalTable: "recipes",
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
                name: "fk_recipes_to_tools_recipes_recipe_id",
                table: "recipes_to_tools",
                column: "recipe_id",
                principalTable: "recipes",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_categories_categories_category_id",
                table: "recipes_to_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_categories_recipes_recipe_id",
                table: "recipes_to_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tags_recipes_recipe_id",
                table: "recipes_to_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tags_tags_tag_id",
                table: "recipes_to_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tools_recipes_recipe_id",
                table: "recipes_to_tools");

            migrationBuilder.DropForeignKey(
                name: "fk_recipes_to_tools_tools_tool_id",
                table: "recipes_to_tools");

            migrationBuilder.RenameColumn(
                name: "tool_id",
                table: "recipes_to_tools",
                newName: "tools_id");

            migrationBuilder.RenameColumn(
                name: "recipe_id",
                table: "recipes_to_tools",
                newName: "recipes_id");

            migrationBuilder.RenameIndex(
                name: "ix_recipes_to_tools_tool_id",
                table: "recipes_to_tools",
                newName: "ix_recipes_to_tools_tools_id");

            migrationBuilder.RenameColumn(
                name: "tag_id",
                table: "recipes_to_tags",
                newName: "tags_id");

            migrationBuilder.RenameColumn(
                name: "recipe_id",
                table: "recipes_to_tags",
                newName: "recipes_id");

            migrationBuilder.RenameIndex(
                name: "ix_recipes_to_tags_tag_id",
                table: "recipes_to_tags",
                newName: "ix_recipes_to_tags_tags_id");

            migrationBuilder.RenameColumn(
                name: "recipe_id",
                table: "recipes_to_categories",
                newName: "recipes_id");

            migrationBuilder.RenameColumn(
                name: "category_id",
                table: "recipes_to_categories",
                newName: "categories_id");

            migrationBuilder.RenameIndex(
                name: "ix_recipes_to_categories_recipe_id",
                table: "recipes_to_categories",
                newName: "ix_recipes_to_categories_recipes_id");

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_categories_categories_categories_id",
                table: "recipes_to_categories",
                column: "categories_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_categories_recipes_recipes_id",
                table: "recipes_to_categories",
                column: "recipes_id",
                principalTable: "recipes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_tags_recipes_recipes_id",
                table: "recipes_to_tags",
                column: "recipes_id",
                principalTable: "recipes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_tags_tags_tags_id",
                table: "recipes_to_tags",
                column: "tags_id",
                principalTable: "tags",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_tools_recipes_recipes_id",
                table: "recipes_to_tools",
                column: "recipes_id",
                principalTable: "recipes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_recipes_to_tools_tools_tools_id",
                table: "recipes_to_tools",
                column: "tools_id",
                principalTable: "tools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
