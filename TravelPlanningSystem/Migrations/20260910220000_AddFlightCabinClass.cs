using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelPlanningSystem.Data;

#nullable disable

namespace TravelPlanningSystem.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260910220000_AddFlightCabinClass")]
public partial class AddFlightCabinClass : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(
            name: "CabinClass",
            table: "FlightBookingSegments",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "Economy");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(
            name: "CabinClass",
            table: "FlightBookingSegments");
}
