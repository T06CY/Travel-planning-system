using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelPlanningSystem.Data;
#nullable disable
namespace TravelPlanningSystem.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20260910200000_AddFlightDisruptionMessage")]
public partial class AddFlightDisruptionMessage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(name: "DisruptionMessage", table: "FlightBookings", type: "nvarchar(500)", maxLength: 500, nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "DisruptionMessage", table: "FlightBookings");
}
