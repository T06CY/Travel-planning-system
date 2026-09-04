using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelPlanningSystem.Data;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260904000100_EnsureHotelRoomType")]
    public partial class EnsureHotelRoomType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('HotelRooms', 'RoomType') IS NULL
BEGIN
    ALTER TABLE [HotelRooms]
    ADD [RoomType] nvarchar(30) NOT NULL
        CONSTRAINT [DF_HotelRooms_RoomType] DEFAULT N'Master Room';
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('HotelRooms', 'RoomType') IS NOT NULL
BEGIN
    DECLARE @constraintName nvarchar(128);
    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'HotelRooms')
      AND c.name = N'RoomType';

    IF @constraintName IS NOT NULL
        EXEC(N'ALTER TABLE [HotelRooms] DROP CONSTRAINT [' + @constraintName + N']');

    ALTER TABLE [HotelRooms] DROP COLUMN [RoomType];
END");
        }
    }
}
