using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TravelPlanningSystem.Data;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260906170000_AddReturnSeatNumber")]
    public partial class AddReturnSeatNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnSeatNumber",
                table: "FlightPassengers",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnSeatNumber",
                table: "FlightPassengers");
        }
    }
}
