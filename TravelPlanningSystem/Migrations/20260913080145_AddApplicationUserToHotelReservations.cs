using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserToHotelReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisruptionMessage",
                table: "ActivityBookings");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationUserId",
                table: "HotelReservations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercent",
                table: "Flights",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

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

            migrationBuilder.AddColumn<string>(
                name: "DisruptionMessage",
                table: "FlightBookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelReservations_ApplicationUserId",
                table: "HotelReservations",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_HotelReservations_Users_ApplicationUserId",
                table: "HotelReservations",
                column: "ApplicationUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HotelReservations_Users_ApplicationUserId",
                table: "HotelReservations");

            migrationBuilder.DropIndex(
                name: "IX_HotelReservations_ApplicationUserId",
                table: "HotelReservations");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "HotelReservations");

            migrationBuilder.DropColumn(
                name: "EstimatedArrivalTime",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "EstimatedDepartureTime",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "DisruptionMessage",
                table: "FlightBookings");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercent",
                table: "Flights",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "DisruptionMessage",
                table: "ActivityBookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
