using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMealPlanHouseholdId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Python stored household_id as an association proxy through users, so the column
            // was added by InitialSchema but not populated for existing records. Fill it in now.
            migrationBuilder.Sql("""
                UPDATE group_meal_plans
                SET household_id = (
                    SELECT u.household_id
                    FROM users u
                    WHERE u.id = group_meal_plans.user_id
                )
                WHERE household_id IS NULL OR household_id = '' OR household_id = '00000000-0000-0000-0000-000000000000';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed for a data backfill.
        }
    }
}
