using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    /// <inheritdoc />
    public partial class AutoAdd_PendingModelChanges_20260904 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safely drop RoomType only if it exists to avoid errors when DB is out of sync
            migrationBuilder.Sql(@"IF EXISTS (
    SELECT * FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'[dbo].[HotelRooms]') AND [name] = N'RoomType'
)
BEGIN
    ALTER TABLE [HotelRooms] DROP COLUMN [RoomType]
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoomType",
                table: "HotelRooms",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }
    }
}
