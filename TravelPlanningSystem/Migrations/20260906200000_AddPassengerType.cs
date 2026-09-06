using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelPlanningSystem.Data;

#nullable disable

namespace TravelPlanningSystem.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260906200000_AddPassengerType")]
public partial class AddPassengerType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(name: "PassengerType", table: "FlightPassengers", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Adult");
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "PassengerType", table: "FlightPassengers");
}
