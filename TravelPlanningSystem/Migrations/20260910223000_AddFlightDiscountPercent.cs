using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelPlanningSystem.Data;

#nullable disable

namespace TravelPlanningSystem.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260910223000_AddFlightDiscountPercent")]
public partial class AddFlightDiscountPercent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<decimal>(
            name: "DiscountPercent",
            table: "Flights",
            type: "decimal(5,2)",
            precision: 5,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(
            name: "DiscountPercent",
            table: "Flights");
}
