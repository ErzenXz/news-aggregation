using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsAggregation.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "UserPreferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "UserPreferences",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
