using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    /// <inheritdoc />
    public partial class BackfillHotelRoomRoomTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [HotelRooms] SET [RoomType] = N'Master Room' WHERE [RoomType] IS NULL;");
            migrationBuilder.Sql("ALTER TABLE [HotelRooms] ALTER COLUMN [RoomType] nvarchar(30) NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [HotelRooms] ALTER COLUMN [RoomType] nvarchar(30) NULL;");
        }
    }
}
