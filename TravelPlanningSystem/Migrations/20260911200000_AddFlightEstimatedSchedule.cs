using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TravelPlanningSystem.Data;

#nullable disable

namespace TravelPlanningSystem.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260911200000_AddFlightEstimatedSchedule")]
public partial class AddFlightEstimatedSchedule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "EstimatedArrivalTime",
            table: "Flights",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "EstimatedDepartureTime",
            table: "Flights",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "EstimatedArrivalTime", table: "Flights");
        migrationBuilder.DropColumn(name: "EstimatedDepartureTime", table: "Flights");
    }
}
