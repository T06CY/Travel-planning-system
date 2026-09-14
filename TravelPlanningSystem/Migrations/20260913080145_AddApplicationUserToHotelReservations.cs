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
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HotelReservations]') AND name = 'ApplicationUserId')
                BEGIN
                    ALTER TABLE [dbo].[HotelReservations] ADD [ApplicationUserId] uniqueidentifier NULL;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Flights]') AND name = 'DiscountPercent')
                BEGIN
                    DECLARE @var nvarchar(max);
                    SELECT @var = QUOTENAME([d].[name])
                    FROM [sys].[default_constraints] [d]
                    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Flights]') AND [c].[name] = N'DiscountPercent');
                    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Flights] DROP CONSTRAINT ' + @var + ';');
                    ALTER TABLE [Flights] ALTER COLUMN [DiscountPercent] decimal(18,2) NOT NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FlightBookings]') AND name = 'DisruptionMessage')
                BEGIN
                    ALTER TABLE [dbo].[FlightBookings] ADD [DisruptionMessage] nvarchar(500) NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HotelReservations_ApplicationUserId' AND object_id = OBJECT_ID(N'[dbo].[HotelReservations]'))
                BEGIN
                    CREATE NONCLUSTERED INDEX [IX_HotelReservations_ApplicationUserId] ON [dbo].[HotelReservations] ([ApplicationUserId]);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HotelReservations_Users_ApplicationUserId')
                BEGIN
                    ALTER TABLE [dbo].[HotelReservations] WITH CHECK ADD CONSTRAINT [FK_HotelReservations_Users_ApplicationUserId] 
                    FOREIGN KEY([ApplicationUserId]) REFERENCES [dbo].[Users] ([UserId]);
                END
            ");
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
                name: "DisruptionMessage",
                table: "FlightBookings");
        }
    }
}