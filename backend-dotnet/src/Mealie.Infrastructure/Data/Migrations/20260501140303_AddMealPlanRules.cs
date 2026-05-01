using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMealPlanRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "group_meal_plan_rules",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    group_id = table.Column<string>(type: "TEXT", nullable: false),
                    household_id = table.Column<string>(type: "TEXT", nullable: true),
                    day = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "unset"),
                    entry_type = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "unset"),
                    query_filter_string = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_meal_plan_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_meal_plan_rules_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_rules_to_categories",
                columns: table => new
                {
                    group_plan_rule_id = table.Column<string>(type: "TEXT", nullable: false),
                    category_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_rules_to_categories", x => new { x.group_plan_rule_id, x.category_id });
                    table.ForeignKey(
                        name: "fk_plan_rules_to_categories_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_plan_rules_to_categories_group_meal_plan_rules_group_plan_rule_id",
                        column: x => x.group_plan_rule_id,
                        principalTable: "group_meal_plan_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_rules_to_households",
                columns: table => new
                {
                    group_plan_rule_id = table.Column<string>(type: "TEXT", nullable: false),
                    household_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_rules_to_households", x => new { x.group_plan_rule_id, x.household_id });
                    table.ForeignKey(
                        name: "fk_plan_rules_to_households_group_meal_plan_rules_group_plan_rule_id",
                        column: x => x.group_plan_rule_id,
                        principalTable: "group_meal_plan_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_plan_rules_to_households_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_rules_to_tags",
                columns: table => new
                {
                    plan_rule_id = table.Column<string>(type: "TEXT", nullable: false),
                    tag_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_rules_to_tags", x => new { x.plan_rule_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_plan_rules_to_tags_group_meal_plan_rules_plan_rule_id",
                        column: x => x.plan_rule_id,
                        principalTable: "group_meal_plan_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_plan_rules_to_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_group_meal_plan_rules_group_id",
                table: "group_meal_plan_rules",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_meal_plan_rules_household_id",
                table: "group_meal_plan_rules",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_plan_rules_to_categories_category_id",
                table: "plan_rules_to_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_plan_rules_to_households_household_id",
                table: "plan_rules_to_households",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_plan_rules_to_tags_tag_id",
                table: "plan_rules_to_tags",
                column: "tag_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_rules_to_categories");

            migrationBuilder.DropTable(
                name: "plan_rules_to_households");

            migrationBuilder.DropTable(
                name: "plan_rules_to_tags");

            migrationBuilder.DropTable(
                name: "group_meal_plan_rules");
        }
    }
}
