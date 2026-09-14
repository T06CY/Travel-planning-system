using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models.Transportation;
using TravelPlanningSystem.Services;

using TransportRoute = TravelPlanningSystem.Models.Transportation.Route;

var builder = WebApplication.CreateBuilder(args);

// 1. Service Registrations
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<IEmailSender, GmailEmailSender>();
builder.Services.AddSingleton<OtpService>();

builder.Services.Configure<RealTimeFlightOptions>(
    builder.Configuration.GetSection(RealTimeFlightOptions.SectionName));
builder.Services.AddHttpClient<IRealTimeFlightService, AeroDataBoxFlightService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddHttpClient<IAirportLookupService, AeroDataBoxAirportService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

// 2. Authentication Configuration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// 3. Database Context Registration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// 4. Database Migration, Schema Self-Healing, and Data Seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception migrationException)
        {
            Console.Error.WriteLine("Database migration notice: " + migrationException.Message);
        }

        // Schema compatibility patches across modules
        await context.Database.ExecuteSqlRawAsync(@"
            -- Hotel Module
            IF OBJECT_ID(N'[dbo].[HotelRooms]', N'U') IS NOT NULL 
               AND COL_LENGTH(N'dbo.HotelRooms', N'RoomType') IS NULL
                ALTER TABLE [dbo].[HotelRooms] ADD [RoomType] nvarchar(max) NULL;

            IF OBJECT_ID(N'[dbo].[Rooms]', N'U') IS NOT NULL 
               AND COL_LENGTH(N'dbo.Rooms', N'RoomType') IS NULL
                ALTER TABLE [dbo].[Rooms] ADD [RoomType] nvarchar(max) NULL;

            IF OBJECT_ID(N'[dbo].[HotelReservations]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.HotelReservations', N'RewardPointsAwarded') IS NULL
                    ALTER TABLE [dbo].[HotelReservations] ADD [RewardPointsAwarded] bit NOT NULL CONSTRAINT [DF_HotelReservations_RewardPointsAwarded] DEFAULT 0;

                IF COL_LENGTH(N'dbo.HotelReservations', N'ApplicationUserId') IS NULL
                    ALTER TABLE [dbo].[HotelReservations] ADD [ApplicationUserId] uniqueidentifier NULL;

                IF COL_LENGTH(N'dbo.HotelReservations', N'StaffUserId') IS NULL
                    ALTER TABLE [dbo].[HotelReservations] ADD [StaffUserId] uniqueidentifier NULL;
            END

            -- Staff Users Security
            IF OBJECT_ID(N'[dbo].[StaffUsers]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.StaffUsers', N'PhoneNumber') IS NULL
                    ALTER TABLE [dbo].[StaffUsers] ADD [PhoneNumber] nvarchar(20) NULL;

                IF COL_LENGTH(N'dbo.StaffUsers', N'AccessLevel') IS NOT NULL
                    ALTER TABLE [dbo].[StaffUsers] DROP COLUMN [AccessLevel];

                IF COL_LENGTH(N'dbo.StaffUsers', N'FailedLoginAttempts') IS NULL
                    ALTER TABLE [dbo].[StaffUsers] ADD [FailedLoginAttempts] int NOT NULL CONSTRAINT [DF_StaffUsers_FailedAttempts] DEFAULT 0;

                IF COL_LENGTH(N'dbo.StaffUsers', N'LockoutUntil') IS NULL
                    ALTER TABLE [dbo].[StaffUsers] ADD [LockoutUntil] datetime2 NULL;
            END

            -- Application Users Security
            IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.Users', N'FailedLoginAttempts') IS NULL
                    ALTER TABLE [dbo].[Users] ADD [FailedLoginAttempts] int NOT NULL CONSTRAINT [DF_Users_FailedAttempts] DEFAULT 0;

                IF COL_LENGTH(N'dbo.Users', N'LockoutUntil') IS NULL
                    ALTER TABLE [dbo].[Users] ADD [LockoutUntil] datetime2 NULL;
            END

            -- Flight Module
            IF OBJECT_ID(N'[dbo].[Flights]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.Flights', N'DiscountPercent') IS NULL
                    ALTER TABLE [dbo].[Flights] ADD [DiscountPercent] decimal(5,2) NOT NULL CONSTRAINT [DF_Flights_DiscountPercent] DEFAULT 0;

                IF COL_LENGTH(N'dbo.Flights', N'EstimatedDepartureTime') IS NULL
                    ALTER TABLE [dbo].[Flights] ADD [EstimatedDepartureTime] datetime2 NULL;

                IF COL_LENGTH(N'dbo.Flights', N'EstimatedArrivalTime') IS NULL
                    ALTER TABLE [dbo].[Flights] ADD [EstimatedArrivalTime] datetime2 NULL;
            END

            IF OBJECT_ID(N'[dbo].[FlightBookings]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.FlightBookings', N'DisruptionMessage') IS NULL
                ALTER TABLE [dbo].[FlightBookings] ADD [DisruptionMessage] nvarchar(500) NULL;

            IF OBJECT_ID(N'[dbo].[FlightBookingSegments]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.FlightBookingSegments', N'CabinClass') IS NULL
                ALTER TABLE [dbo].[FlightBookingSegments] ADD [CabinClass] nvarchar(30) NOT NULL CONSTRAINT [DF_FlightBookingSegments_CabinClass] DEFAULT N'Economy';

            -- Transportation Module Cancellation Fields
            IF OBJECT_ID(N'[dbo].[TransportationBookings]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.TransportationBookings', N'CancellationReason') IS NULL
                    ALTER TABLE [dbo].[TransportationBookings] ADD [CancellationReason] nvarchar(400) NULL;

                IF COL_LENGTH(N'dbo.TransportationBookings', N'CancelledAt') IS NULL
                    ALTER TABLE [dbo].[TransportationBookings] ADD [CancelledAt] datetime2 NULL;
            END
        ");

        await SeedData.InitializeAsync(context);
        await SeedTransportationDataAsync(context);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Database initialization error: " + ex.Message);
    }
}

// 5. Diagnostics Endpoints
app.MapGet("/diagnostics/migrations", (AppDbContext db) =>
{
    var applied = db.Database.GetAppliedMigrations();
    var pending = db.Database.GetPendingMigrations();
    return Results.Json(new { Applied = applied, Pending = pending });
});

app.MapGet("/diagnostics/counts", async (AppDbContext db) =>
{
    try
    {
        var users = await db.Users.CountAsync();
        var staff = await db.StaffUsers.CountAsync();
        var roles = await db.StaffRoles.CountAsync();
        return Results.Json(new { Users = users, StaffUsers = staff, StaffRoles = roles });
    }
    catch (Exception ex)
    {
        return Results.Problem("Error reading counts: " + ex.Message);
    }
});

app.MapPost("/diagnostics/forceseed", async (AppDbContext db) =>
{
    try
    {
        var staffUsers = await db.StaffUsers.ToListAsync();
        if (staffUsers.Any()) db.StaffUsers.RemoveRange(staffUsers);

        var roles = await db.StaffRoles.ToListAsync();
        if (roles.Any()) db.StaffRoles.RemoveRange(roles);

        var users = await db.Users.ToListAsync();
        if (users.Any()) db.Users.RemoveRange(users);

        await db.SaveChangesAsync();

        await SeedData.InitializeAsync(db);

        var afterUsers = await db.Users.CountAsync();
        var afterStaff = await db.StaffUsers.CountAsync();
        var afterRoles = await db.StaffRoles.CountAsync();

        return Results.Json(new { Users = afterUsers, StaffUsers = afterStaff, StaffRoles = afterRoles });
    }
    catch (Exception ex)
    {
        return Results.Problem("Force seed error: " + ex.Message);
    }
});

// 6. HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// =========================================================================
// Transportation Seed Data (16 intercity schedules across 8 major routes)
// =========================================================================
async Task SeedTransportationDataAsync(AppDbContext context)
{
    if (context.Trips.Count() >= 15)
        return;

    if (context.Routes.Any())
    {
        if (context.TransportationReviews.Any())
            context.TransportationReviews.RemoveRange(context.TransportationReviews);

        context.Seats.RemoveRange(context.Seats);
        context.Trips.RemoveRange(context.Trips);
        context.Vehicles.RemoveRange(context.Vehicles);
        context.Routes.RemoveRange(context.Routes);
        await context.SaveChangesAsync();
    }

    var routes = new List<TransportRoute>
    {
        new() { Origin = "Kuala Lumpur", Destination = "Genting Highlands", DistanceKm = 55, EstimatedDurationHours = 1.0, Stops = "Awana Skyway", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { Origin = "Kuala Lumpur", Destination = "Cameron Highlands", DistanceKm = 215, EstimatedDurationHours = 3.5, Stops = "Tapah, Tanah Rata", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { Origin = "Kuala Lumpur", Destination = "Melaka", DistanceKm = 145, EstimatedDurationHours = 2.0, Stops = "Direct Express", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { Origin = "Kuala Lumpur", Destination = "Penang", DistanceKm = 360, EstimatedDurationHours = 4.5, Stops = "Ipoh, Butterworth", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { Origin = "Kuala Lumpur", Destination = "Johor Bahru", DistanceKm = 330, EstimatedDurationHours = 4.0, Stops = "Seremban, Batu Pahat", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { Origin = "Kuala Lumpur", Destination = "Ipoh", DistanceKm = 205, EstimatedDurationHours = 2.5, Stops = "Tanjung Malim", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { Origin = "Penang", Destination = "Kuala Lumpur", DistanceKm = 360, EstimatedDurationHours = 4.5, Stops = "Butterworth, Ipoh", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { Origin = "Melaka", Destination = "Kuala Lumpur", DistanceKm = 145, EstimatedDurationHours = 2.0, Stops = "Direct Express", IsActive = true, CreatedAt = DateTime.UtcNow }
    };
    context.Routes.AddRange(routes);
    await context.SaveChangesAsync();

    var vehicles = new List<Vehicle>
    {
        new() { LicensePlate = "WXY-1199", VehicleModel = "Toyota Hiace VIP Luxury", VehicleType = "VIP Van", SeatingCapacity = 12, Amenities = "Air Conditioning, Leather Seats, Fast Shuttle", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { LicensePlate = "VIP-8888", VehicleModel = "Toyota Alphard Executive", VehicleType = "VIP Van", SeatingCapacity = 7, Amenities = "First-Class Leather Seats, Personal USB", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { LicensePlate = "WVE-8801", VehicleModel = "Mercedes-Benz Tourismo (2+1 VIP)", VehicleType = "Luxury Coach", SeatingCapacity = 30, Amenities = "WiFi, Massage Seats, Restroom", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { LicensePlate = "VAA-6622", VehicleModel = "Scania K410IB High-Deck", VehicleType = "Express Bus", SeatingCapacity = 45, Amenities = "WiFi, Air Conditioning, USB Charging", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { LicensePlate = "BPN-3311", VehicleModel = "Volvo B11R Executive", VehicleType = "Coach", SeatingCapacity = 36, Amenities = "WiFi, Restroom, Power Outlets", IsActive = true, CreatedAt = DateTime.UtcNow }
    };
    context.Vehicles.AddRange(vehicles);
    await context.SaveChangesAsync();

    var today = DateTime.UtcNow.Date;

    var tripDefinitions = new (int rIdx, int vIdx, int h, int m, decimal fare, int discount, int avail, int total, string status)[]
    {
        (0, 0, 9, 0, 35m, 0, 8, 12, "On-time"),
        (0, 1, 14, 30, 45m, 10, 5, 7, "Scheduled"),
        (1, 0, 8, 30, 50m, 0, 6, 12, "On-time"),
        (2, 0, 10, 30, 40m, 5, 7, 12, "On-time"),
        (5, 1, 15, 0, 42m, 0, 4, 7, "Scheduled"),
        (3, 2, 8, 0, 55m, 0, 20, 30, "On-time"),
        (3, 3, 13, 30, 45m, 15, 3, 45, "Scheduled"),
        (3, 4, 20, 0, 48m, 0, 25, 36, "Scheduled"),
        (2, 2, 14, 0, 28m, 0, 16, 30, "On-time"),
        (2, 3, 17, 30, 25m, 0, 30, 45, "Scheduled"),
        (4, 2, 11, 0, 52m, 0, 14, 30, "On-time"),
        (4, 3, 18, 0, 48m, 0, 10, 45, "Delayed"),
        (6, 2, 9, 30, 55m, 0, 18, 30, "On-time"),
        (6, 3, 16, 0, 45m, 10, 22, 45, "Scheduled"),
        (7, 4, 11, 30, 28m, 0, 15, 36, "On-time"),
        (0, 0, 18, 30, 35m, 0, 10, 12, "On-time")
    };

    var trips = tripDefinitions.Select(def =>
    {
        var route = routes.ElementAt(def.rIdx);
        var depTime = today.AddHours(def.h).AddMinutes(def.m);
        var durationHours = Math.Max(1.0, route.EstimatedDurationHours);
        var arrTime = depTime.AddHours(durationHours);

        return new Trip
        {
            RouteId = route.RouteId,
            VehicleId = vehicles.ElementAt(def.vIdx).VehicleId,
            DepartureTime = depTime,
            ArrivalTime = arrTime,
            BaseFare = def.fare,
            DiscountPercentage = def.discount,
            AvailableSeats = def.avail,
            TotalSeats = def.total,
            Status = def.status,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }).ToList();

    context.Trips.AddRange(trips);
    await context.SaveChangesAsync();

    Console.WriteLine("Transportation seed data initialized with 16 scheduled departures.");
}