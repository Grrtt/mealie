using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingListUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite requires a full table rebuild to add a FK column.
            // We do it manually so the migration is idempotent against the partially-applied
            // state (where ADD COLUMN user_id already ran but the table rebuild failed).
            migrationBuilder.Sql("PRAGMA foreign_keys = 0;");

            migrationBuilder.Sql(@"
                CREATE TABLE ef_temp_shopping_lists (
                    id TEXT NOT NULL CONSTRAINT pk_shopping_lists PRIMARY KEY,
                    name TEXT NOT NULL,
                    group_id TEXT NOT NULL,
                    household_id TEXT NOT NULL,
                    user_id TEXT NOT NULL DEFAULT '',
                    created_at TEXT NOT NULL,
                    update_at TEXT NOT NULL,
                    CONSTRAINT fk_shopping_lists_groups_group_id
                        FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE CASCADE,
                    CONSTRAINT fk_shopping_lists_households_household_id
                        FOREIGN KEY (household_id) REFERENCES households (id) ON DELETE RESTRICT,
                    CONSTRAINT fk_shopping_lists_users_user_id
                        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
                );");

            // Backfill: assign the first admin user (or first user) to all existing lists
            migrationBuilder.Sql(@"
                INSERT INTO ef_temp_shopping_lists
                    (id, name, group_id, household_id, user_id, created_at, update_at)
                SELECT
                    s.id, s.name, s.group_id, s.household_id,
                    COALESCE(
                        (SELECT u.id FROM users u WHERE u.admin = 1 ORDER BY u.created_at LIMIT 1),
                        (SELECT u.id FROM users u ORDER BY u.created_at LIMIT 1)
                    ),
                    s.created_at, s.update_at
                FROM shopping_lists s;");

            migrationBuilder.Sql("DROP TABLE shopping_lists;");
            migrationBuilder.Sql("ALTER TABLE ef_temp_shopping_lists RENAME TO shopping_lists;");

            migrationBuilder.Sql("PRAGMA foreign_keys = 1;");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_shopping_lists_user_id ON shopping_lists (user_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = 0;");

            migrationBuilder.Sql(@"
                CREATE TABLE ef_temp_shopping_lists (
                    id TEXT NOT NULL CONSTRAINT pk_shopping_lists PRIMARY KEY,
                    name TEXT NOT NULL,
                    group_id TEXT NOT NULL,
                    household_id TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    update_at TEXT NOT NULL,
                    CONSTRAINT fk_shopping_lists_groups_group_id
                        FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE CASCADE,
                    CONSTRAINT fk_shopping_lists_households_household_id
                        FOREIGN KEY (household_id) REFERENCES households (id) ON DELETE RESTRICT
                );");

            migrationBuilder.Sql(@"
                INSERT INTO ef_temp_shopping_lists (id, name, group_id, household_id, created_at, update_at)
                SELECT id, name, group_id, household_id, created_at, update_at
                FROM shopping_lists;");

            migrationBuilder.Sql("DROP TABLE shopping_lists;");
            migrationBuilder.Sql("ALTER TABLE ef_temp_shopping_lists RENAME TO shopping_lists;");

            migrationBuilder.Sql("PRAGMA foreign_keys = 1;");
        }
    }
}
