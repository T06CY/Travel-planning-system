using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStaffAccessLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "StaffUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "StaffUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
