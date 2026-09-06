using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models.Transportation;
using TravelPlanningSystem.ViewModels;
using TransportRoute = TravelPlanningSystem.Models.Transportation.Route;

namespace TravelPlanningSystem.Controllers;

[Authorize]
public class AdminTransportationController(AppDbContext context) : Controller
{
    // ==========================================
    // 后台管理主看板 (Dashboard & Management Hub)
    // ==========================================
    public async Task<IActionResult> Index()
    {
        var model = new AdminTransportationViewModel
        {
            TotalVehicles = await context.Vehicles.CountAsync(v => v.IsActive),
            TotalRoutes = await context.Routes.CountAsync(r => r.IsActive),
            ActiveTripsCount = await context.Trips.CountAsync(t => t.IsActive && t.DepartureTime >= DateTime.UtcNow.Date),
            TotalTicketsSold = await context.TransportationPassengers.CountAsync(),
            TotalRevenue = await context.TransportationBookings.Where(b => b.BookingStatus == "Confirmed").SumAsync(b => b.TotalAmount),

            Vehicles = await context.Vehicles.OrderByDescending(v => v.CreatedAt).ToListAsync(),
            Routes = await context.Routes.OrderBy(r => r.Origin).ThenBy(r => r.Destination).ToListAsync(),
            Trips = await context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle)
                .OrderByDescending(t => t.DepartureTime)
                .Take(20)
                .ToListAsync(),
            RecentBookings = await context.TransportationBookings
                .Include(b => b.Trip).ThenInclude(t => t!.Route)
                .Include(b => b.Passengers)
                .OrderByDescending(b => b.BookingDate)
                .Take(10)
                .ToListAsync()
        };

        return View(model);
    }

    // ==========================================
    // 1. 车队管理：录入新车辆 (POST)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVehicle(AdminTransportationViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(vm.NewVehicle.LicensePlate))
        {
            var vehicle = new Vehicle
            {
                LicensePlate = vm.NewVehicle.LicensePlate.Trim().ToUpper(),
                VehicleModel = vm.NewVehicle.VehicleModel.Trim(),
                VehicleType = vm.NewVehicle.VehicleType,
                SeatingCapacity = Math.Max(4, vm.NewVehicle.SeatingCapacity),
                ManufactureYear = vm.NewVehicle.ManufactureYear,
                Amenities = vm.NewVehicle.Amenities,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Vehicle '{vehicle.LicensePlate} - {vehicle.VehicleModel}' added to fleet successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // 2. 路线调度：创建新路线 (POST)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoute(AdminTransportationViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(vm.NewRoute.Origin) && !string.IsNullOrWhiteSpace(vm.NewRoute.Destination))
        {
            var route = new TransportRoute
            {
                Origin = vm.NewRoute.Origin.Trim(),
                Destination = vm.NewRoute.Destination.Trim(),
                DistanceKm = vm.NewRoute.DistanceKm > 0 ? vm.NewRoute.DistanceKm : 100,
                EstimatedDurationHours = vm.NewRoute.EstimatedDurationHours > 0 ? vm.NewRoute.EstimatedDurationHours : 2.0,
                Stops = string.IsNullOrWhiteSpace(vm.NewRoute.Stops) ? "Direct" : vm.NewRoute.Stops.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Routes.Add(route);
            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"New Route '{route.Origin} → {route.Destination}' established!";
        }
        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // 3. 排班调度：发布新车次 (POST)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTrip(AdminTransportationViewModel vm)
    {
        var route = await context.Routes.FindAsync(vm.NewTrip.RouteId);
        var vehicle = await context.Vehicles.FindAsync(vm.NewTrip.VehicleId);

        if (route != null && vehicle != null)
        {
            var depTime = vm.NewTrip.DepartureTime;
            var arrTime = depTime.AddHours(Math.Max(1.0, route.EstimatedDurationHours));

            var trip = new Trip
            {
                RouteId = route.RouteId,
                VehicleId = vehicle.VehicleId,
                DepartureTime = depTime,
                ArrivalTime = arrTime,
                BaseFare = Math.Max(5.00m, vm.NewTrip.BaseFare),
                DiscountPercentage = Math.Clamp(vm.NewTrip.DiscountPercentage, 0, 90),
                TotalSeats = vehicle.SeatingCapacity,
                AvailableSeats = vehicle.SeatingCapacity,
                Status = "Scheduled",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // 自动为新发布的车次铺设座位表
            for (int i = 1; i <= vehicle.SeatingCapacity; i++)
            {
                int row = (i - 1) / 3 + 1;
                char col = (char)('A' + ((i - 1) % 3));
                trip.Seats.Add(new Seat
                {
                    SeatNumber = $"{row}{col}",
                    SeatClass = i <= 3 ? "VIP" : "Standard",
                    SeatType = ((i - 1) % 3 == 0) ? "Window" : "Aisle",
                    IsAvailable = true,
                    Status = "Available",
                    CreatedAt = DateTime.UtcNow
                });
            }

            context.Trips.Add(trip);
            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Trip for {route.Origin} → {route.Destination} ({depTime:HH:mm, dd MMM}) published successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // 4. 乘客登车名单 (Passenger Manifest)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Manifest(int tripId)
    {
        var trip = await context.Trips
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Seats)
            .FirstOrDefaultAsync(t => t.TripId == tripId);

        if (trip == null)
            return NotFound("Trip not found.");

        var bookings = await context.TransportationBookings
            .Include(b => b.Passengers)
            .Where(b => b.TripId == tripId && b.BookingStatus == "Confirmed")
            .ToListAsync();

        ViewBag.Trip = trip;
        return View(bookings);
    }

    // ==========================================
    // 5. 取消/下架车次 (POST)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTripStatus(int id)
    {
        var trip = await context.Trips.FindAsync(id);
        if (trip != null)
        {
            trip.Status = (trip.Status == "Cancelled") ? "Scheduled" : "Cancelled";
            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Trip #{trip.TripId} status updated to {trip.Status}!";
        }
        return RedirectToAction(nameof(Index));
    }
}