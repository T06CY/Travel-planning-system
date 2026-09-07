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
            migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HotelRooms]') AND name = 'RoomType')
            BEGIN
                ALTER TABLE [dbo].[HotelRooms] ADD [RoomType] nvarchar(max) NULL;
            END

            EXEC(N'UPDATE [HotelRooms] SET [RoomType] = N''Master Room'' WHERE [RoomType] IS NULL;');
        ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [HotelRooms] ALTER COLUMN [RoomType] nvarchar(30) NULL;");
        }
    }
}
