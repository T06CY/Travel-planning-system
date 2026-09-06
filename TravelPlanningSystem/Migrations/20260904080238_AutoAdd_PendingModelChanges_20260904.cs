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
            // Safely drop RoomType column with its constraint if it exists
            migrationBuilder.Sql(@"IF EXISTS (
    SELECT * FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'[dbo].[HotelRooms]') AND [name] = N'RoomType'
)
BEGIN
    -- First, drop the default constraint if it exists
    IF EXISTS (
        SELECT * FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID(N'[dbo].[HotelRooms]')
        AND name = N'DF_HotelRooms_RoomType'
    )
    BEGIN
        ALTER TABLE [HotelRooms] DROP CONSTRAINT [DF_HotelRooms_RoomType]
    END

    -- Then drop the column
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
