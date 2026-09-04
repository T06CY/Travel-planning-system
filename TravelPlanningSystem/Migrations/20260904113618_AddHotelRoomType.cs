using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    public partial class AddHotelRoomType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RoomType was already added by EnsureHotelRoomType migration.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No action required.
        }
    }
}