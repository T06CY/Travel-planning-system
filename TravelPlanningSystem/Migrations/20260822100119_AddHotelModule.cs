using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HotelRooms",
                columns: table => new
                {
                    HotelRoomId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RoomName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PricePerNight = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    TotalRooms = table.Column<int>(type: "int", nullable: false),
                    Amenities = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StarRating = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelRooms", x => x.HotelRoomId);
                });

            migrationBuilder.CreateTable(
                name: "HotelReservations",
                columns: table => new
                {
                    HotelReservationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationReference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HotelRoomId = table.Column<int>(type: "int", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    PricePerNight = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ReservationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReservationStatus = table.Column<int>(type: "int", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelReservations", x => x.HotelReservationId);
                    table.ForeignKey(
                        name: "FK_HotelReservations_HotelRooms_HotelRoomId",
                        column: x => x.HotelRoomId,
                        principalTable: "HotelRooms",
                        principalColumn: "HotelRoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HotelRoomPhotos",
                columns: table => new
                {
                    HotelRoomPhotoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelRoomId = table.Column<int>(type: "int", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelRoomPhotos", x => x.HotelRoomPhotoId);
                    table.ForeignKey(
                        name: "FK_HotelRoomPhotos_HotelRooms_HotelRoomId",
                        column: x => x.HotelRoomId,
                        principalTable: "HotelRooms",
                        principalColumn: "HotelRoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HotelReviews",
                columns: table => new
                {
                    HotelReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelRoomId = table.Column<int>(type: "int", nullable: false),
                    HotelReservationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelReviews", x => x.HotelReviewId);
                    table.ForeignKey(
                        name: "FK_HotelReviews_HotelReservations_HotelReservationId",
                        column: x => x.HotelReservationId,
                        principalTable: "HotelReservations",
                        principalColumn: "HotelReservationId");
                    table.ForeignKey(
                        name: "FK_HotelReviews_HotelRooms_HotelRoomId",
                        column: x => x.HotelRoomId,
                        principalTable: "HotelRooms",
                        principalColumn: "HotelRoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HotelReservations_HotelRoomId",
                table: "HotelReservations",
                column: "HotelRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelReviews_HotelReservationId",
                table: "HotelReviews",
                column: "HotelReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelReviews_HotelRoomId",
                table: "HotelReviews",
                column: "HotelRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelRoomPhotos_HotelRoomId",
                table: "HotelRoomPhotos",
                column: "HotelRoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HotelReviews");

            migrationBuilder.DropTable(
                name: "HotelRoomPhotos");

            migrationBuilder.DropTable(
                name: "HotelReservations");

            migrationBuilder.DropTable(
                name: "HotelRooms");
        }
    }
}
