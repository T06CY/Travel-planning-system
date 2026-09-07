using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelWithDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AddonFee",
                table: "FlightBookings",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BaggageOption",
                table: "FlightBookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "FlightBookings",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "HasTravelInsurance",
                table: "FlightBookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "FlightBookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "FlightBookings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PromoCode",
                table: "FlightBookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddonFee",
                table: "FlightBookings");

            migrationBuilder.DropColumn(
                name: "BaggageOption",
                table: "FlightBookings");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "FlightBookings");

            migrationBuilder.DropColumn(
                name: "HasTravelInsurance",
                table: "FlightBookings");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "FlightBookings");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "FlightBookings");

            migrationBuilder.DropColumn(
                name: "PromoCode",
                table: "FlightBookings");
        }
    }
}
