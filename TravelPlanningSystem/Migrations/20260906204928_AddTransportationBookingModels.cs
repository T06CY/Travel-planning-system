using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportationBookingModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransportationBookings",
                columns: table => new
                {
                    BookingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BaseFareTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaggageFeeTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InsuranceFeeTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PromoCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BookingStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportationBookings", x => x.BookingId);
                    table.ForeignKey(
                        name: "FK_TransportationBookings_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransportationBookings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportationPassengers",
                columns: table => new
                {
                    PassengerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PassengerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SeatId = table.Column<int>(type: "int", nullable: true),
                    SeatNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BaggageOption = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BaggagePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HasTravelInsurance = table.Column<bool>(type: "bit", nullable: false),
                    InsurancePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SpecialRequests = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportationPassengers", x => x.PassengerId);
                    table.ForeignKey(
                        name: "FK_TransportationPassengers_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "SeatId");
                    table.ForeignKey(
                        name: "FK_TransportationPassengers_TransportationBookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "TransportationBookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransportationBookings_TripId",
                table: "TransportationBookings",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportationBookings_UserId",
                table: "TransportationBookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportationPassengers_BookingId",
                table: "TransportationPassengers",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportationPassengers_SeatId",
                table: "TransportationPassengers",
                column: "SeatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransportationPassengers");

            migrationBuilder.DropTable(
                name: "TransportationBookings");
        }
    }
}
