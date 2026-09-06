using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelPlanningSystem.Data;

#nullable disable

namespace TravelPlanningSystem.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260906180000_AddFlightLogoUrl")]
public partial class AddFlightLogoUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "FlightLogoUrl", table: "Flights", type: "nvarchar(350)", maxLength: 350, nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "FlightLogoUrl", table: "Flights");
}
