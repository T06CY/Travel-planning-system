using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelPlanningSystem.Data;

#nullable disable

namespace TravelPlanningSystem.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260906150000_AddAirlineReservationModule")]
public partial class AddAirlineReservationModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Airlines",
            columns: table => new
            {
                AirlineId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AirlineCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                AirlineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Country = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                LogoUrl = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: true),
                SupportEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                ContactNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Airlines", x => x.AirlineId);
            });

        migrationBuilder.CreateTable(
            name: "Airports",
            columns: table => new
            {
                AirportId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AirportCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                AirportName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Country = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Airports", x => x.AirportId);
            });

        migrationBuilder.CreateTable(
            name: "FlightBookings",
            columns: table => new
            {
                FlightBookingId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BookingReference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                UserEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                TripType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ContactName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                ContactEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CancellationReason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FlightBookings", x => x.FlightBookingId);
            });

        migrationBuilder.CreateTable(
            name: "Flights",
            columns: table => new
            {
                FlightId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AirlineId = table.Column<int>(type: "int", nullable: false),
                FlightNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                From = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                To = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                DepartureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                SeatCapacity = table.Column<int>(type: "int", nullable: false),
                AvailableSeats = table.Column<int>(type: "int", nullable: false),
                AircraftModel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Flights", x => x.FlightId);
                table.ForeignKey(
                    name: "FK_Flights_Airlines_AirlineId",
                    column: x => x.AirlineId,
                    principalTable: "Airlines",
                    principalColumn: "AirlineId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "FlightPassengers",
            columns: table => new
            {
                FlightPassengerId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FlightBookingId = table.Column<int>(type: "int", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                PassportNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Nationality = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                SeatNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                MealPreference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FlightPassengers", x => x.FlightPassengerId);
                table.ForeignKey(
                    name: "FK_FlightPassengers_FlightBookings_FlightBookingId",
                    column: x => x.FlightBookingId,
                    principalTable: "FlightBookings",
                    principalColumn: "FlightBookingId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "FlightBookingSegments",
            columns: table => new
            {
                FlightBookingSegmentId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FlightBookingId = table.Column<int>(type: "int", nullable: false),
                FlightId = table.Column<int>(type: "int", nullable: false),
                SegmentOrder = table.Column<int>(type: "int", nullable: false),
                PricePerPassenger = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FlightBookingSegments", x => x.FlightBookingSegmentId);
                table.ForeignKey(
                    name: "FK_FlightBookingSegments_FlightBookings_FlightBookingId",
                    column: x => x.FlightBookingId,
                    principalTable: "FlightBookings",
                    principalColumn: "FlightBookingId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_FlightBookingSegments_Flights_FlightId",
                    column: x => x.FlightId,
                    principalTable: "Flights",
                    principalColumn: "FlightId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Airlines_AirlineCode",
            table: "Airlines",
            column: "AirlineCode",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Airports_AirportCode",
            table: "Airports",
            column: "AirportCode",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_FlightBookings_BookingReference",
            table: "FlightBookings",
            column: "BookingReference",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Flights_AirlineId",
            table: "Flights",
            column: "AirlineId");
        migrationBuilder.CreateIndex(
            name: "IX_Flights_FlightNumber_DepartureTime",
            table: "Flights",
            columns: new[] { "FlightNumber", "DepartureTime" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_FlightPassengers_FlightBookingId",
            table: "FlightPassengers",
            column: "FlightBookingId");
        migrationBuilder.CreateIndex(
            name: "IX_FlightBookingSegments_FlightBookingId_SegmentOrder",
            table: "FlightBookingSegments",
            columns: new[] { "FlightBookingId", "SegmentOrder" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_FlightBookingSegments_FlightId",
            table: "FlightBookingSegments",
            column: "FlightId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Airports");
        migrationBuilder.DropTable(name: "FlightBookingSegments");
        migrationBuilder.DropTable(name: "FlightPassengers");
        migrationBuilder.DropTable(name: "Flights");
        migrationBuilder.DropTable(name: "FlightBookings");
        migrationBuilder.DropTable(name: "Airlines");
    }
}
