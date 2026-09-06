using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelPlanningSystem.Data;

#nullable disable

namespace TravelPlanningSystem.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260906210000_CompleteAirlineImages")]
public partial class CompleteAirlineImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"UPDATE Airlines SET LogoUrl = CASE AirlineCode
            WHEN 'MH' THEN '/images/flights/MalaysiaAirlines.png'
            WHEN 'AK' THEN '/images/flights/AirAsia.png'
            WHEN 'OD' THEN '/images/flights/BatikAir.png'
            WHEN 'SQ' THEN '/images/flights/SingaporeAirlines.png'
            WHEN 'TG' THEN '/images/flights/ThaiAirways.png'
            WHEN 'JL' THEN '/images/flights/JapanAirlines.png'
            ELSE LogoUrl END;");
        migrationBuilder.Sql(@"UPDATE f SET FlightLogoUrl = a.LogoUrl
            FROM Flights f INNER JOIN Airlines a ON f.AirlineId = a.AirlineId
            WHERE a.LogoUrl IS NOT NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
