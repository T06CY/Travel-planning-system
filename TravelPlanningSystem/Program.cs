using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models.Transportation;

// 别名避免与 ASP.NET Core 的 Route 冲突
using TransportRoute = TravelPlanningSystem.Models.Transportation.Route;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add cookie authentication to enable simple sign-in for testing
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });

builder.Services.AddAuthorization();

// Register database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- 数据库迁移、自动修复字段与数据初始化 ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await context.Database.MigrateAsync();

        // ⭐ 核心修复：自动给 HotelRooms / Rooms 表补上缺失的 RoomType 字段
        await context.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID(N'[dbo].[HotelRooms]', N'U') IS NOT NULL 
               AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HotelRooms]') AND name = 'RoomType')
            BEGIN
                ALTER TABLE [dbo].[HotelRooms] ADD [RoomType] nvarchar(max) NULL;
            END

            IF OBJECT_ID(N'[dbo].[Rooms]', N'U') IS NOT NULL 
               AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Rooms]') AND name = 'RoomType')
            BEGIN
                ALTER TABLE [dbo].[Rooms] ADD [RoomType] nvarchar(max) NULL;
            END
        ");

        await SeedData.InitializeAsync(context);

        // ⭐ 初始化交通模块种子数据
        await SeedTransportationDataAsync(context);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Database migration/seed error: " + ex);
    }
}

// Diagnostics endpoint to show applied and pending migrations
app.MapGet("/diagnostics/migrations", (AppDbContext db) =>
{
    var applied = db.Database.GetAppliedMigrations();
    var pending = db.Database.GetPendingMigrations();
    return Results.Json(new { Applied = applied, Pending = pending });
});

// Diagnostics endpoint to show counts of seeded tables
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
        Console.Error.WriteLine("Diagnostics counts error: " + ex);
        return Results.Problem("Error reading counts: " + ex.Message);
    }
});

// Force reseed endpoint (POST) - clears Users/StaffUsers/StaffRoles and re-runs seeder
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
        Console.Error.WriteLine("Force seed error: " + ex);
        return Results.Problem("Force seed failed: " + ex.Message);
    }
});

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

// ==========================================
// Transportation 种子数据方法
// ==========================================
async Task SeedTransportationDataAsync(AppDbContext context)
{
    if (context.Routes.Any())
        return;

    // 1. Routes (路线)
    var routes = new List<TransportRoute>
    {
        new() { Origin = "Kuala Lumpur", Destination = "Selangor", DistanceKm = 45, EstimatedDurationHours = 1.5, Stops = "Shah Alam, Petaling Jaya", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { Origin = "Kuala Lumpur", Destination = "Penang", DistanceKm = 380, EstimatedDurationHours = 5, Stops = "Ipoh, Taiping", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { Origin = "Kuala Lumpur", Destination = "Melaka", DistanceKm = 140, EstimatedDurationHours = 2, Stops = "None", IsActive = true, CreatedAt = DateTime.UtcNow }
    };
    context.Routes.AddRange(routes);
    await context.SaveChangesAsync();

    // 2. Vehicles (车辆)
    var vehicles = new List<Vehicle>
    {
        new() { LicensePlate = "KL-001-A", VehicleModel = "Mercedes Sprinter", VehicleType = "Coach", SeatingCapacity = 50, ManufactureYear = 2023, Amenities = "WiFi, Air Conditioning, USB Charging, Toilet", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { LicensePlate = "KL-002-B", VehicleModel = "Volvo B11R", VehicleType = "Bus", SeatingCapacity = 45, ManufactureYear = 2022, Amenities = "WiFi, Air Conditioning, Power Outlets", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { LicensePlate = "KL-003-C", VehicleModel = "Toyota Hiace", VehicleType = "Van", SeatingCapacity = 15, ManufactureYear = 2023, Amenities = "Air Conditioning", IsActive = true, CreatedAt = DateTime.UtcNow }
    };
    context.Vehicles.AddRange(vehicles);
    await context.SaveChangesAsync();

    // 提取 Id
    var r0 = routes[0].RouteId;
    var r1 = routes.ElementAt(1).RouteId;
    var r2 = routes.ElementAt(2).RouteId;

    var v0 = vehicles[0].VehicleId;
    var v1 = vehicles.ElementAt(1).VehicleId;
    var v2 = vehicles.ElementAt(2).VehicleId;

    // 3. Trips (车次)
    var trips = new List<Trip>
    {
        new() { RouteId = r0, VehicleId = v2, DepartureTime = DateTime.UtcNow.AddHours(2).Date.AddHours(8), ArrivalTime = DateTime.UtcNow.AddHours(2).Date.AddHours(9.5), BaseFare = 25.00m, DiscountPercentage = 0, AvailableSeats = 12, TotalSeats = 15, Status = "Scheduled", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { RouteId = r1, VehicleId = v0, DepartureTime = DateTime.UtcNow.AddDays(1).Date.AddHours(8), ArrivalTime = DateTime.UtcNow.AddDays(1).Date.AddHours(13), BaseFare = 55.00m, DiscountPercentage = 0, AvailableSeats = 32, TotalSeats = 50, Status = "Scheduled", IsActive = true, CreatedAt = DateTime.UtcNow },
        new() { RouteId = r2, VehicleId = v1, DepartureTime = DateTime.UtcNow.AddHours(2).Date.AddHours(10), ArrivalTime = DateTime.UtcNow.AddHours(2).Date.AddHours(12), BaseFare = 35.00m, DiscountPercentage = 0, AvailableSeats = 15, TotalSeats = 45, Status = "On-time", IsActive = true, CreatedAt = DateTime.UtcNow }
    };
    context.Trips.AddRange(trips);
    await context.SaveChangesAsync();

    Console.WriteLine("✅ Transportation sample data seeded successfully!");
}