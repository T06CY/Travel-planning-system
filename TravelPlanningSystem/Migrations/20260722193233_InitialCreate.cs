using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityCategories",
                columns: table => new
                {
                    ActivityCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityCategories", x => x.ActivityCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PricePerPerson = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DurationHours = table.Column<double>(type: "float", nullable: false),
                    MinimumParticipants = table.Column<int>(type: "int", nullable: false),
                    MaximumParticipants = table.Column<int>(type: "int", nullable: false),
                    MinimumAge = table.Column<int>(type: "int", nullable: false),
                    IncludedItems = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    WhatToBring = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivityCategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.ActivityId);
                    table.ForeignKey(
                        name: "FK_Activities_ActivityCategories_ActivityCategoryId",
                        column: x => x.ActivityCategoryId,
                        principalTable: "ActivityCategories",
                        principalColumn: "ActivityCategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityPhotos",
                columns: table => new
                {
                    ActivityPhotoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityPhotos", x => x.ActivityPhotoId);
                    table.ForeignKey(
                        name: "FK_ActivityPhotos_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "ActivityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivitySessions",
                columns: table => new
                {
                    ActivitySessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    AvailableSlots = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivitySessions", x => x.ActivitySessionId);
                    table.ForeignKey(
                        name: "FK_ActivitySessions_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "ActivityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityBookings",
                columns: table => new
                {
                    ActivityBookingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingReference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ActivitySessionId = table.Column<int>(type: "int", nullable: false),
                    ParticipantCount = table.Column<int>(type: "int", nullable: false),
                    PricePerPerson = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BookingStatus = table.Column<int>(type: "int", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityBookings", x => x.ActivityBookingId);
                    table.ForeignKey(
                        name: "FK_ActivityBookings_ActivitySessions_ActivitySessionId",
                        column: x => x.ActivitySessionId,
                        principalTable: "ActivitySessions",
                        principalColumn: "ActivitySessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityReviews",
                columns: table => new
                {
                    ActivityReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    ActivityBookingId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    ActivityBookingId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityReviews", x => x.ActivityReviewId);
                    table.ForeignKey(
                        name: "FK_ActivityReviews_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "ActivityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityReviews_ActivityBookings_ActivityBookingId",
                        column: x => x.ActivityBookingId,
                        principalTable: "ActivityBookings",
                        principalColumn: "ActivityBookingId");
                    table.ForeignKey(
                        name: "FK_ActivityReviews_ActivityBookings_ActivityBookingId1",
                        column: x => x.ActivityBookingId1,
                        principalTable: "ActivityBookings",
                        principalColumn: "ActivityBookingId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActivityCategoryId",
                table: "Activities",
                column: "ActivityCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityBookings_ActivitySessionId",
                table: "ActivityBookings",
                column: "ActivitySessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPhotos_ActivityId",
                table: "ActivityPhotos",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityReviews_ActivityBookingId",
                table: "ActivityReviews",
                column: "ActivityBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityReviews_ActivityBookingId1",
                table: "ActivityReviews",
                column: "ActivityBookingId1",
                unique: true,
                filter: "[ActivityBookingId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityReviews_ActivityId",
                table: "ActivityReviews",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySessions_ActivityId",
                table: "ActivitySessions",
                column: "ActivityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityPhotos");

            migrationBuilder.DropTable(
                name: "ActivityReviews");

            migrationBuilder.DropTable(
                name: "ActivityBookings");

            migrationBuilder.DropTable(
                name: "ActivitySessions");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "ActivityCategories");
        }
    }
}
