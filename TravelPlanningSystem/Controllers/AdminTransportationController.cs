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
    // =========================================================================
    // Core Module 3: Transportation Operations Dashboard & KPI Hub
    // =========================================================================
    [HttpGet]
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

    // =========================================================================
    // Core Module 3: Fleet Management - Register New Vehicle (POST)
    // =========================================================================
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

    // =========================================================================
    // Core Module 3: Route Network - Establish New Route (POST)
    // =========================================================================
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
                // DistanceKm is of type decimal in the entity model
                DistanceKm = (decimal)(vm.NewRoute.DistanceKm > 0 ? vm.NewRoute.DistanceKm : 100),
                // EstimatedDurationHours is of type double in the entity model
                EstimatedDurationHours = (double)(vm.NewRoute.EstimatedDurationHours > 0 ? vm.NewRoute.EstimatedDurationHours : 2.0),
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

    // =========================================================================
    // Core Module 3: Schedule Orchestration - Publish New Trip & Seats (POST)
    // =========================================================================
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

            // Automatically generate the visual seat map for the scheduled trip
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

    // =========================================================================
    // Core Module 3: Dispatch Operations - Passenger Boarding Manifest (GET)
    // =========================================================================
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
            .Where(b => b.TripId == tripId)
            .ToListAsync();

        ViewBag.Trip = trip;
        return View(bookings);
    }

    // =========================================================================
    // Core Module 3: Administrative Ticket Cancellation with Audit Reason (POST)
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(int bookingId, string cancellationReason, int? returnTripId = null)
    {
        // 1. Validate that the administrator provided a valid cancellation explanation
        if (string.IsNullOrWhiteSpace(cancellationReason) || cancellationReason.Trim().Length < 5)
        {
            TempData["ErrorMessage"] = "Please provide an administrative cancellation reason (minimum 5 characters).";
            return returnTripId.HasValue
                ? RedirectToAction(nameof(Manifest), new { tripId = returnTripId.Value })
                : RedirectToAction(nameof(Index));
        }

        // 2. Fetch target booking with associated trip seats and passengers
        var booking = await context.TransportationBookings
            .Include(b => b.Trip).ThenInclude(t => t!.Seats)
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null)
            return NotFound("Transportation booking not found.");

        if (booking.BookingStatus == "Cancelled")
        {
            TempData["ErrorMessage"] = $"Booking {booking.BookingReference} has already been cancelled.";
            return returnTripId.HasValue
                ? RedirectToAction(nameof(Manifest), new { tripId = returnTripId.Value })
                : RedirectToAction(nameof(Index));
        }

        // 3. Mark booking as cancelled and record administrative audit details
        var adminName = User.Identity?.Name ?? "Administrator";
        booking.BookingStatus = "Cancelled";
        booking.PaymentStatus = "Refunded";
        booking.CancellationReason = $"[Admin Cancelled by {adminName}]: {cancellationReason.Trim()}";
        booking.CancelledAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        // 4. Release physical seats back to active inventory
        if (booking.Trip != null)
        {
            var seatNumbers = booking.Passengers.Select(p => p.SeatNumber).ToList();
            var seatsToRelease = booking.Trip.Seats
                .Where(s => seatNumbers.Contains(s.SeatNumber))
                .ToList();

            foreach (var seat in seatsToRelease)
            {
                seat.IsAvailable = true;
                seat.Status = "Available";
            }

            booking.Trip.AvailableSeats += booking.Passengers.Count;
        }

        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Ticket {booking.BookingReference} has been cancelled by Administrator. Reserved seats have been released.";

        return returnTripId.HasValue
            ? RedirectToAction(nameof(Manifest), new { tripId = returnTripId.Value })
            : RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // Core Module 3: Toggle Trip Status (Schedule / Cancel Whole Trip) (POST)
    // =========================================================================
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

    // =========================================================================
    // Core Module 3: Data Export 1 - Passenger Boarding Manifest (CSV)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> ExportManifest(int tripId)
    {
        var trip = await context.Trips
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.TripId == tripId);

        if (trip == null) return NotFound();

        var bookings = await context.TransportationBookings
            .Include(b => b.Passengers)
            .Where(b => b.TripId == tripId && b.BookingStatus == "Confirmed")
            .ToListAsync();

        var builder = new System.Text.StringBuilder();
        // UTF-8 BOM ensures that Microsoft Excel displays characters correctly without encoding issues
        builder.AppendLine("Seat,Passenger Name,IC/Passport,Type,Baggage,Insurance,Booking Reference,Contact Phone");

        foreach (var b in bookings)
        {
            foreach (var p in b.Passengers)
            {
                builder.AppendLine($"\"{p.SeatNumber}\",\"{p.FullName}\",\"{p.IdNumber}\",\"{p.PassengerType}\",\"{p.BaggageOption}\",\"{(p.HasTravelInsurance ? "Yes" : "No")}\",\"{b.BookingReference}\",\"{b.ContactPhone}\"");
            }
        }

        byte[] buffer = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        string filename = $"Manifest_Trip_{tripId}_{trip.Route?.Origin}_to_{trip.Route?.Destination}_{DateTime.UtcNow:yyyyMMdd}.csv";

        return File(buffer, "text/csv", filename);
    }

    // =========================================================================
    // Core Module 3: Data Export 2 - Operations Revenue & Booking Summary (CSV)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> ExportRevenueReport()
    {
        var bookings = await context.TransportationBookings
            .AsNoTracking()
            .Include(b => b.Trip).ThenInclude(t => t!.Route)
            .Include(b => b.Trip).ThenInclude(t => t!.Vehicle)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Booking Reference,Booking Date,Route,Vehicle,Seats Count,Base Fare,Discount,Total Paid,Status,Payment Method");

        foreach (var b in bookings)
        {
            builder.AppendLine($"\"{b.BookingReference}\",\"{b.BookingDate:yyyy-MM-dd HH:mm}\",\"{b.Trip?.Route?.Origin} -> {b.Trip?.Route?.Destination}\",\"{b.Trip?.Vehicle?.VehicleModel}\",\"{b.Passengers.Count}\",\"{b.BaseFareTotal}\",\"{b.DiscountAmount}\",\"{b.TotalAmount}\",\"{b.BookingStatus}\",\"{b.PaymentMethod}\"");
        }

        byte[] buffer = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        return File(buffer, "text/csv", $"Transportation_Revenue_Summary_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // =========================================================================
    // Core Module 3: Real-time Check-in Boarding Status (AJAX POST)
    // =========================================================================
    [HttpPost]
    public async Task<IActionResult> ToggleBoarding(int passengerId, bool isBoarded)
    {
        var passenger = await context.TransportationPassengers.FindAsync(passengerId);
        if (passenger == null) return NotFound();

        passenger.IsBoarded = isBoarded;
        passenger.BoardedAt = isBoarded ? DateTime.UtcNow : null;
        await context.SaveChangesAsync();

        return Json(new { success = true, isBoarded = passenger.IsBoarded });
    }
}