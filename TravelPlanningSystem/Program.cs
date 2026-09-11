using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models.Transportation;
using TravelPlanningSystem.Services;

// 使用别名，避免与 ASP.NET Core 的 Route 路由类命名冲突
using TransportRoute = TravelPlanningSystem.Models.Transportation.Route;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
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

// --- 数据库迁移、字段自动修复与数据初始化 ---
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
            // Continue to the idempotent compatibility checks below. This keeps
            // older databases usable even when a migration was partially applied.
            Console.Error.WriteLine("Database migration warning: " + migrationException.Message);
        }

        // 自动给 HotelRooms / Rooms 表补上缺失的 RoomType 字段（解决 Hotel 报错）
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

            IF OBJECT_ID(N'[dbo].[HotelReservations]', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HotelReservations]') AND name = 'RewardPointsAwarded')
            BEGIN
                ALTER TABLE [dbo].[HotelReservations]
                    ADD [RewardPointsAwarded] bit NOT NULL
                        CONSTRAINT [DF_HotelReservations_RewardPointsAwarded] DEFAULT 0;
            END

            IF OBJECT_ID(N'[dbo].[Flights]', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Flights]') AND name = 'DiscountPercent')
            BEGIN
                ALTER TABLE [dbo].[Flights]
                    ADD [DiscountPercent] decimal(5,2) NOT NULL
                        CONSTRAINT [DF_Flights_DiscountPercent] DEFAULT 0;
            END

            IF OBJECT_ID(N'[dbo].[FlightBookings]', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FlightBookings]') AND name = 'DisruptionMessage')
            BEGIN
                ALTER TABLE [dbo].[FlightBookings] ADD [DisruptionMessage] nvarchar(500) NULL;
            END
        ");

        await SeedData.InitializeAsync(context);

        // 初始化/更新 Transportation 演示数据（含 VIP Van）
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
// Transportation 扩充版演示数据（共 16 趟，支持分页测试）
// ==========================================
async Task SeedTransportationDataAsync(AppDbContext context)
{
    // 如果已经有 15 趟以上车次，说明已是最新完整数据，直接跳过
    if (context.Trips.Count() >= 15)
        return;

    // 清理旧数据
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

    // 1. 路线 (Routes)
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

    // 2. 车辆 (Vehicles)
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

    // 3. 车次数据配置表 (路线编号, 车辆编号, 出发时, 出发分, 票价, 折扣%, 余票, 总票, 状态)
    var tripDefinitions = new (int rIdx, int vIdx, int h, int m, decimal fare, int discount, int avail, int total, string status)[]
    {
        // 5 趟 VIP Van 专线
        (0, 0, 9, 0, 35m, 0, 8, 12, "On-time"),           // KL -> 云顶 (Hiace)
        (0, 1, 14, 30, 45m, 10, 5, 7, "Scheduled"),      // KL -> 云顶 (Alphard)
        (1, 0, 8, 30, 50m, 0, 6, 12, "On-time"),          // KL -> 金马仑 (Hiace)
        (2, 0, 10, 30, 40m, 5, 7, 12, "On-time"),         // KL -> 马六甲 (Hiace)
        (5, 1, 15, 0, 42m, 0, 4, 7, "Scheduled"),        // KL -> 怡保 (Alphard)

        // 3 趟 KL -> 槟城 (早/中/晚)
        (3, 2, 8, 0, 55m, 0, 20, 30, "On-time"),
        (3, 3, 13, 30, 45m, 15, 3, 45, "Scheduled"),
        (3, 4, 20, 0, 48m, 0, 25, 36, "Scheduled"),

        // 2 趟 KL -> 马六甲
        (2, 2, 14, 0, 28m, 0, 16, 30, "On-time"),
        (2, 3, 17, 30, 25m, 0, 30, 45, "Scheduled"),

        // 2 趟 KL -> 新山
        (4, 2, 11, 0, 52m, 0, 14, 30, "On-time"),
        (4, 3, 18, 0, 48m, 0, 10, 45, "Delayed"),

        // 2 趟 槟城 -> KL (返程)
        (6, 2, 9, 30, 55m, 0, 18, 30, "On-time"),
        (6, 3, 16, 0, 45m, 10, 22, 45, "Scheduled"),

        // 2 趟 补充班次（冲破 12 条限制以激活第 2 页）
        (7, 4, 11, 30, 28m, 0, 15, 36, "On-time"),        // 马六甲 -> KL 返程
        (0, 0, 18, 30, 35m, 0, 10, 12, "On-time")         // KL -> 云顶 傍晚 Van
    };

    // 批量生成 16 趟车次实体
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

    Console.WriteLine("✅ 已成功注入 16 趟完整班程，分页与各类车型演示已就绪！");
}
