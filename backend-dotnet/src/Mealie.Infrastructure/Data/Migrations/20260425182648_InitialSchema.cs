using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "server_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: true),
                    log = table.Column<string>(type: "TEXT", nullable: true),
                    household_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_server_tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    slug = table.Column<string>(type: "TEXT", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    private_group = table.Column<bool>(type: "INTEGER", nullable: false),
                    first_day_of_week = table.Column<string>(type: "TEXT", nullable: true),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_preferences_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "households",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "TEXT", nullable: true),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_households", x => x.id);
                    table.ForeignKey(
                        name: "fk_households_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredient_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    abbreviation = table.Column<string>(type: "TEXT", nullable: true),
                    plural_name = table.Column<string>(type: "TEXT", nullable: true),
                    plural_abbreviation = table.Column<string>(type: "TEXT", nullable: true),
                    use_abbreviation = table.Column<bool>(type: "INTEGER", nullable: false),
                    fraction = table.Column<bool>(type: "INTEGER", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredient_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredient_units_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invite_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    token = table.Column<string>(type: "TEXT", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    household_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invite_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_invite_tokens_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "multi_purpose_labels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    color = table.Column<string>(type: "TEXT", nullable: true),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_multi_purpose_labels", x => x.id);
                    table.ForeignKey(
                        name: "fk_multi_purpose_labels_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    slug = table.Column<string>(type: "TEXT", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_tags_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    slug = table.Column<string>(type: "TEXT", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    on_hand = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tools", x => x.id);
                    table.ForeignKey(
                        name: "fk_tools_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cookbooks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    image = table.Column<string>(type: "TEXT", nullable: true),
                    @public = table.Column<bool>(name: "public", type: "INTEGER", nullable: false),
                    require_all_categories = table.Column<bool>(type: "INTEGER", nullable: false),
                    position = table.Column<int>(type: "INTEGER", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    household_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cookbooks", x => x.id);
                    table.ForeignKey(
                        name: "fk_cookbooks_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cookbooks_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_events_notifiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    appris_url = table.Column<string>(type: "TEXT", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    household_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_events_notifiers", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_events_notifiers_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_events_notifiers_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "household_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    private_household = table.Column<bool>(type: "INTEGER", nullable: false),
                    first_day_of_week = table.Column<string>(type: "TEXT", nullable: true),
                    recipe_public = table.Column<string>(type: "TEXT", nullable: true),
                    recipe_show_nutrition = table.Column<string>(type: "TEXT", nullable: true),
                    recipe_show_assets = table.Column<string>(type: "TEXT", nullable: true),
                    recipe_landscape_view = table.Column<string>(type: "TEXT", nullable: true),
                    recipe_disable_comments = table.Column<string>(type: "TEXT", nullable: true),
                    recipe_disable_amount = table.Column<string>(type: "TEXT", nullable: true),
                    household_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_household_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_household_preferences_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    recipe_yield = table.Column<string>(type: "TEXT", nullable: true),
                    total_time = table.Column<string>(type: "TEXT", nullable: true),
                    prep_time = table.Column<string>(type: "TEXT", nullable: true),
                    cook_time = table.Column<string>(type: "TEXT", nullable: true),
                    perform_time = table.Column<string>(type: "TEXT", nullable: true),
                    rating = table.Column<int>(type: "INTEGER", nullable: true),
                    is_ocr = table.Column<bool>(type: "INTEGER", nullable: false),
                    disable_amount = table.Column<bool>(type: "INTEGER", nullable: false),
                    image = table.Column<string>(type: "TEXT", nullable: true),
                    org_url = table.Column<string>(type: "TEXT", nullable: true),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    household_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_made = table.Column<DateTime>(type: "TEXT", nullable: true),
                    nutrition_calories = table.Column<string>(type: "TEXT", nullable: true),
                    nutrition_fat_content = table.Column<string>(type: "TEXT", nullable: true),
                    nutrition_protein_content = table.Column<string>(type: "TEXT", nullable: true),
                    nutrition_carbohydrate_content = table.Column<string>(type: "TEXT", nullable: true),
                    nutrition_fiber_content = table.Column<string>(type: "TEXT", nullable: true),
                    nutrition_sodium_content = table.Column<string>(type: "TEXT", nullable: true),
                    nutrition_sugar_content = table.Column<string>(type: "TEXT", nullable: true),
                    settings_public = table.Column<bool>(type: "INTEGER", nullable: true),
                    settings_show_nutrition = table.Column<bool>(type: "INTEGER", nullable: true),
                    settings_show_assets = table.Column<bool>(type: "INTEGER", nullable: true),
                    settings_landscape_view = table.Column<bool>(type: "INTEGER", nullable: true),
                    settings_disable_comments = table.Column<bool>(type: "INTEGER", nullable: true),
                    settings_disable_amount = table.Column<bool>(type: "INTEGER", nullable: true),
                    settings_locked = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipes", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipes_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipes_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shopping_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    household_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_lists", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopping_lists_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shopping_lists_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    full_name = table.Column<string>(type: "TEXT", nullable: true),
                    username = table.Column<string>(type: "TEXT", nullable: true),
                    email = table.Column<string>(type: "TEXT", nullable: true),
                    password = table.Column<string>(type: "TEXT", nullable: true),
                    auth_method = table.Column<string>(type: "TEXT", nullable: false),
                    admin = table.Column<bool>(type: "INTEGER", nullable: false),
                    advanced = table.Column<bool>(type: "INTEGER", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    household_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    cache_key = table.Column<string>(type: "TEXT", nullable: true),
                    login_attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    locked_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    can_manage_household = table.Column<bool>(type: "INTEGER", nullable: false),
                    can_manage = table.Column<bool>(type: "INTEGER", nullable: false),
                    can_invite = table.Column<bool>(type: "INTEGER", nullable: false),
                    can_organize = table.Column<bool>(type: "INTEGER", nullable: false),
                    show_announcements = table.Column<bool>(type: "INTEGER", nullable: false),
                    last_read_announcement = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_users_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "webhook_urls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    url = table.Column<string>(type: "TEXT", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    scheduled_time = table.Column<string>(type: "TEXT", nullable: true),
                    method = table.Column<string>(type: "TEXT", nullable: true),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    household_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_urls", x => x.id);
                    table.ForeignKey(
                        name: "fk_webhook_urls_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_webhook_urls_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ingredient_foods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    plural_name = table.Column<string>(type: "TEXT", nullable: true),
                    unit_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    on_hand = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredient_foods", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredient_foods_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ingredient_foods_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "ingredient_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cookbooks_to_categories",
                columns: table => new
                {
                    categories_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    cookbook_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cookbooks_to_categories", x => new { x.categories_id, x.cookbook_id });
                    table.ForeignKey(
                        name: "fk_cookbooks_to_categories_categories_categories_id",
                        column: x => x.categories_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cookbooks_to_categories_cookbooks_cookbook_id",
                        column: x => x.cookbook_id,
                        principalTable: "cookbooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cookbooks_to_tags",
                columns: table => new
                {
                    cookbook_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tags_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cookbooks_to_tags", x => new { x.cookbook_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_cookbooks_to_tags_cookbooks_cookbook_id",
                        column: x => x.cookbook_id,
                        principalTable: "cookbooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cookbooks_to_tags_tags_tags_id",
                        column: x => x.tags_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cookbooks_to_tools",
                columns: table => new
                {
                    cookbook_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tools_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cookbooks_to_tools", x => new { x.cookbook_id, x.tools_id });
                    table.ForeignKey(
                        name: "fk_cookbooks_to_tools_cookbooks_cookbook_id",
                        column: x => x.cookbook_id,
                        principalTable: "cookbooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cookbooks_to_tools_tools_tools_id",
                        column: x => x.tools_id,
                        principalTable: "tools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_events_notifier_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    test_message = table.Column<bool>(type: "INTEGER", nullable: false),
                    recipe_created = table.Column<bool>(type: "INTEGER", nullable: false),
                    recipe_updated = table.Column<bool>(type: "INTEGER", nullable: false),
                    recipe_deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    user_signup = table.Column<bool>(type: "INTEGER", nullable: false),
                    mealplan_entry_created = table.Column<bool>(type: "INTEGER", nullable: false),
                    shopping_list_created = table.Column<bool>(type: "INTEGER", nullable: false),
                    shopping_list_updated = table.Column<bool>(type: "INTEGER", nullable: false),
                    shopping_list_deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_events_notifier_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_events_notifier_options_group_events_notifiers_id",
                        column: x => x.id,
                        principalTable: "group_events_notifiers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    text = table.Column<string>(type: "TEXT", nullable: false),
                    recipe_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_notes_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    icon = table.Column<string>(type: "TEXT", nullable: false),
                    extension = table.Column<string>(type: "TEXT", nullable: false),
                    recipe_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_assets_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_instructions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    position = table.Column<int>(type: "INTEGER", nullable: false),
                    text = table.Column<string>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: true),
                    summary = table.Column<string>(type: "TEXT", nullable: true),
                    recipe_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ingredient_references = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_instructions", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_instructions_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_share_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recipe_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_share_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_share_tokens_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipe_share_tokens_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_timeline_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    subject = table.Column<string>(type: "TEXT", nullable: true),
                    event_type = table.Column<string>(type: "TEXT", nullable: true),
                    event_message = table.Column<string>(type: "TEXT", nullable: true),
                    recipe_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_timeline_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_timeline_events_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipes_to_categories",
                columns: table => new
                {
                    categories_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recipes_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipes_to_categories", x => new { x.categories_id, x.recipes_id });
                    table.ForeignKey(
                        name: "fk_recipes_to_categories_categories_categories_id",
                        column: x => x.categories_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipes_to_categories_recipes_recipes_id",
                        column: x => x.recipes_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipes_to_tags",
                columns: table => new
                {
                    recipes_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tags_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipes_to_tags", x => new { x.recipes_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_recipes_to_tags_recipes_recipes_id",
                        column: x => x.recipes_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipes_to_tags_tags_tags_id",
                        column: x => x.tags_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipes_to_tools",
                columns: table => new
                {
                    recipes_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tools_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipes_to_tools", x => new { x.recipes_id, x.tools_id });
                    table.ForeignKey(
                        name: "fk_recipes_to_tools_recipes_recipes_id",
                        column: x => x.recipes_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipes_to_tools_tools_tools_id",
                        column: x => x.tools_id,
                        principalTable: "tools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shopping_list_recipe_reference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    shopping_list_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recipe_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recipe_scale = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_list_recipe_reference", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopping_list_recipe_reference_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shopping_list_recipe_reference_shopping_lists_shopping_list_id",
                        column: x => x.shopping_list_id,
                        principalTable: "shopping_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_meal_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    text = table.Column<string>(type: "TEXT", nullable: true),
                    entry_type = table.Column<string>(type: "TEXT", nullable: false),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    recipe_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    household_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_meal_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_meal_plans_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_meal_plans_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_meal_plans_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_group_meal_plans_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "long_live_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    token = table.Column<string>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_long_live_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_long_live_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    text = table.Column<string>(type: "TEXT", nullable: false),
                    recipe_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_comments_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipe_comments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users_to_recipes",
                columns: table => new
                {
                    favorite_recipes_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users_to_recipes", x => new { x.favorite_recipes_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_users_to_recipes_recipes_favorite_recipes_id",
                        column: x => x.favorite_recipes_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_users_to_recipes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredient_foods_aliases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    food_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredient_foods_aliases", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredient_foods_aliases_foods_food_id",
                        column: x => x.food_id,
                        principalTable: "ingredient_foods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipes_ingredients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    position = table.Column<int>(type: "INTEGER", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: true),
                    note = table.Column<string>(type: "TEXT", nullable: true),
                    quantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    unit_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    food_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    original_text = table.Column<string>(type: "TEXT", nullable: true),
                    is_food = table.Column<bool>(type: "INTEGER", nullable: false),
                    disable_amount = table.Column<bool>(type: "INTEGER", nullable: false),
                    recipe_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipes_ingredients", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipes_ingredients_ingredient_foods_food_id",
                        column: x => x.food_id,
                        principalTable: "ingredient_foods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_recipes_ingredients_ingredient_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "ingredient_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_recipes_ingredients_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shopping_list_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    note = table.Column<string>(type: "TEXT", nullable: true),
                    is_food = table.Column<bool>(type: "INTEGER", nullable: false),
                    @checked = table.Column<bool>(name: "checked", type: "INTEGER", nullable: false),
                    disable_amount = table.Column<bool>(type: "INTEGER", nullable: false),
                    quantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    shopping_list_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    unit_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    food_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    label_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    position = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_list_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopping_list_items_foods_food_id",
                        column: x => x.food_id,
                        principalTable: "ingredient_foods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_shopping_list_items_labels_label_id",
                        column: x => x.label_id,
                        principalTable: "multi_purpose_labels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_shopping_list_items_shopping_lists_shopping_list_id",
                        column: x => x.shopping_list_id,
                        principalTable: "shopping_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shopping_list_items_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "ingredient_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "shopping_list_item_recipe_reference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    shopping_list_item_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recipe_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recipe_quantity_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    recipe_quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    recipe_scale = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_list_item_recipe_reference", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopping_list_item_recipe_reference_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shopping_list_item_recipe_reference_shopping_list_items_shopping_list_item_id",
                        column: x => x.shopping_list_item_id,
                        principalTable: "shopping_list_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_group_id",
                table: "categories",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_cookbooks_group_id",
                table: "cookbooks",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_cookbooks_household_id",
                table: "cookbooks",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_cookbooks_to_categories_cookbook_id",
                table: "cookbooks_to_categories",
                column: "cookbook_id");

            migrationBuilder.CreateIndex(
                name: "ix_cookbooks_to_tags_tags_id",
                table: "cookbooks_to_tags",
                column: "tags_id");

            migrationBuilder.CreateIndex(
                name: "ix_cookbooks_to_tools_tools_id",
                table: "cookbooks_to_tools",
                column: "tools_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_events_notifiers_group_id",
                table: "group_events_notifiers",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_events_notifiers_household_id",
                table: "group_events_notifiers",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_meal_plans_date",
                table: "group_meal_plans",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_group_meal_plans_group_id",
                table: "group_meal_plans",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_meal_plans_household_id",
                table: "group_meal_plans",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_meal_plans_recipe_id",
                table: "group_meal_plans",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_meal_plans_user_id",
                table: "group_meal_plans",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_preferences_group_id",
                table: "group_preferences",
                column: "group_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_groups_name",
                table: "groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_groups_slug",
                table: "groups",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_household_preferences_household_id",
                table: "household_preferences",
                column: "household_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_households_group_id",
                table: "households",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_foods_group_id",
                table: "ingredient_foods",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_foods_unit_id",
                table: "ingredient_foods",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_foods_aliases_food_id",
                table: "ingredient_foods_aliases",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_units_group_id",
                table: "ingredient_units",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_invite_tokens_group_id",
                table: "invite_tokens",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_long_live_tokens_token",
                table: "long_live_tokens",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "ix_long_live_tokens_user_id",
                table: "long_live_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_multi_purpose_labels_group_id",
                table: "multi_purpose_labels",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_recipe_id",
                table: "notes",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_assets_recipe_id",
                table: "recipe_assets",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_comments_recipe_id",
                table: "recipe_comments",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_comments_user_id",
                table: "recipe_comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_instructions_recipe_id",
                table: "recipe_instructions",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_share_tokens_group_id",
                table: "recipe_share_tokens",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_share_tokens_recipe_id",
                table: "recipe_share_tokens",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_timeline_events_recipe_id",
                table: "recipe_timeline_events",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_group_id",
                table: "recipes",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_household_id",
                table: "recipes",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_slug_group_id",
                table: "recipes",
                columns: new[] { "slug", "group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipes_ingredients_food_id",
                table: "recipes_ingredients",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_ingredients_recipe_id",
                table: "recipes_ingredients",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_ingredients_unit_id",
                table: "recipes_ingredients",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_to_categories_recipes_id",
                table: "recipes_to_categories",
                column: "recipes_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_to_tags_tags_id",
                table: "recipes_to_tags",
                column: "tags_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_to_tools_tools_id",
                table: "recipes_to_tools",
                column: "tools_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_item_recipe_reference_recipe_id",
                table: "shopping_list_item_recipe_reference",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_item_recipe_reference_shopping_list_item_id",
                table: "shopping_list_item_recipe_reference",
                column: "shopping_list_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_food_id",
                table: "shopping_list_items",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_label_id",
                table: "shopping_list_items",
                column: "label_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_shopping_list_id",
                table: "shopping_list_items",
                column: "shopping_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_unit_id",
                table: "shopping_list_items",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_recipe_reference_recipe_id",
                table: "shopping_list_recipe_reference",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_recipe_reference_shopping_list_id",
                table: "shopping_list_recipe_reference",
                column: "shopping_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_lists_group_id",
                table: "shopping_lists",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_lists_household_id",
                table: "shopping_lists",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_group_id",
                table: "tags",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_tools_group_id",
                table: "tools",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_full_name",
                table: "users",
                column: "full_name");

            migrationBuilder.CreateIndex(
                name: "ix_users_group_id",
                table: "users",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_household_id",
                table: "users",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_to_recipes_user_id",
                table: "users_to_recipes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_urls_group_id",
                table: "webhook_urls",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_urls_household_id",
                table: "webhook_urls",
                column: "household_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cookbooks_to_categories");

            migrationBuilder.DropTable(
                name: "cookbooks_to_tags");

            migrationBuilder.DropTable(
                name: "cookbooks_to_tools");

            migrationBuilder.DropTable(
                name: "group_events_notifier_options");

            migrationBuilder.DropTable(
                name: "group_meal_plans");

            migrationBuilder.DropTable(
                name: "group_preferences");

            migrationBuilder.DropTable(
                name: "household_preferences");

            migrationBuilder.DropTable(
                name: "ingredient_foods_aliases");

            migrationBuilder.DropTable(
                name: "invite_tokens");

            migrationBuilder.DropTable(
                name: "long_live_tokens");

            migrationBuilder.DropTable(
                name: "notes");

            migrationBuilder.DropTable(
                name: "recipe_assets");

            migrationBuilder.DropTable(
                name: "recipe_comments");

            migrationBuilder.DropTable(
                name: "recipe_instructions");

            migrationBuilder.DropTable(
                name: "recipe_share_tokens");

            migrationBuilder.DropTable(
                name: "recipe_timeline_events");

            migrationBuilder.DropTable(
                name: "recipes_ingredients");

            migrationBuilder.DropTable(
                name: "recipes_to_categories");

            migrationBuilder.DropTable(
                name: "recipes_to_tags");

            migrationBuilder.DropTable(
                name: "recipes_to_tools");

            migrationBuilder.DropTable(
                name: "server_tasks");

            migrationBuilder.DropTable(
                name: "shopping_list_item_recipe_reference");

            migrationBuilder.DropTable(
                name: "shopping_list_recipe_reference");

            migrationBuilder.DropTable(
                name: "users_to_recipes");

            migrationBuilder.DropTable(
                name: "webhook_urls");

            migrationBuilder.DropTable(
                name: "cookbooks");

            migrationBuilder.DropTable(
                name: "group_events_notifiers");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "tools");

            migrationBuilder.DropTable(
                name: "shopping_list_items");

            migrationBuilder.DropTable(
                name: "recipes");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "ingredient_foods");

            migrationBuilder.DropTable(
                name: "multi_purpose_labels");

            migrationBuilder.DropTable(
                name: "shopping_lists");

            migrationBuilder.DropTable(
                name: "ingredient_units");

            migrationBuilder.DropTable(
                name: "households");

            migrationBuilder.DropTable(
                name: "groups");
        }
    }
}
