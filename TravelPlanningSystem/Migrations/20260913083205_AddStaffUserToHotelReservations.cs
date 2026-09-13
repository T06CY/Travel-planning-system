using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffUserToHotelReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StaffUserId",
                table: "HotelReservations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelReservations_StaffUserId",
                table: "HotelReservations",
                column: "StaffUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_HotelReservations_StaffUsers_StaffUserId",
                table: "HotelReservations",
                column: "StaffUserId",
                principalTable: "StaffUsers",
                principalColumn: "StaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HotelReservations_StaffUsers_StaffUserId",
                table: "HotelReservations");

            migrationBuilder.DropIndex(
                name: "IX_HotelReservations_StaffUserId",
                table: "HotelReservations");

            migrationBuilder.DropColumn(
                name: "StaffUserId",
                table: "HotelReservations");
        }
    }
}
