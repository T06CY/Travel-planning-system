using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using TravelPlanningSystem.Data;

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

// --- THE FIX IS IN THIS BLOCK ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    // Keep migration check and seed data. Wrap in try/catch so startup does not fail silently
    try
    {
        await context.Database.MigrateAsync();
        await SeedData.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        // Log the exception to console and continue so the app can start for debugging
        Console.Error.WriteLine("Database migration/seed error: " + ex);
    }
}
// --------------------------------

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
        // Remove dependent staff users first to satisfy FK
        var staffUsers = await db.StaffUsers.ToListAsync();
        if (staffUsers.Any()) db.StaffUsers.RemoveRange(staffUsers);

        var roles = await db.StaffRoles.ToListAsync();
        if (roles.Any()) db.StaffRoles.RemoveRange(roles);

        var users = await db.Users.ToListAsync();
        if (users.Any()) db.Users.RemoveRange(users);

        await db.SaveChangesAsync();

        // Rerun seed
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