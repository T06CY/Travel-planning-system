using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanningSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffUserToHotelReservations : Migration
    {
        /// <inheritdoc />
//       protected override void Up(MigrationBuilder migrationBuilder)
//       {
//           migrationBuilder.AddColumn<Guid>(
//               name: "StaffUserId",
//               table: "HotelReservations",
//               type: "uniqueidentifier",
//               nullable: true);
//
//           migrationBuilder.CreateIndex(
//               name: "IX_HotelReservations_StaffUserId",
//               table: "HotelReservations",
//               column: "StaffUserId");
//
//           migrationBuilder.AddForeignKey(
//               name: "FK_HotelReservations_StaffUsers_StaffUserId",
//               table: "HotelReservations",
//               column: "StaffUserId",
//               principalTable: "StaffUsers",
//               principalColumn: "StaffId");
//       }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HotelReservations]') AND name = 'StaffUserId')
                BEGIN
                    ALTER TABLE [dbo].[HotelReservations] ADD [StaffUserId] uniqueidentifier NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HotelReservations_StaffUserId' AND object_id = OBJECT_ID(N'[dbo].[HotelReservations]'))
                BEGIN
                    CREATE NONCLUSTERED INDEX [IX_HotelReservations_StaffUserId] ON [dbo].[HotelReservations] ([StaffUserId]);
                END
            ");
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
