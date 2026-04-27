using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mealie.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodLabelId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"ingredient_foods\" ADD COLUMN \"label_id\" TEXT NULL;");
            migrationBuilder.Sql("CREATE INDEX \"ix_ingredient_foods_label_id\" ON \"ingredient_foods\" (\"label_id\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // SQLite does not support DROP COLUMN — column left in place on rollback
        }
    }
}
