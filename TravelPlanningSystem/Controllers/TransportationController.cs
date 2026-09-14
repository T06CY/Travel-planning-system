using System.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.Models.Transportation;
using TravelPlanningSystem.ViewModels;

// Alias to resolve name collision between ASP.NET Core Routing.Route and Transportation.Route
using TransportRoute = TravelPlanningSystem.Models.Transportation.Route;

namespace TravelPlanningSystem.Controllers;

[Authorize]
public class TransportationController(AppDbContext context, IMemoryCache cache) : Controller
{
    // Memory cache keys for semi-static catalog data
    private const string CacheKeyRoutes = "Transportation_Active_Routes";
    private const string CacheKeyVehicleTypes = "Transportation_Vehicle_Types";

    // =========================================================================
    // Public Catalog & Search Page with High-Performance Memory Caching (Index)
    // =========================================================================
    [AllowAnonymous]
    public async Task<IActionResult> Index(TransportationSearchViewModel model)
    {
        // 1. Ensure valid page number and page size
        model.Page = Math.Max(1, model.Page);
        if (model.PageSize <= 0) model.PageSize = 12;

        // 2. Validate price range constraints
        if (model.MinPrice.HasValue && model.MinPrice.Value < 0)
        {
            ModelState.AddModelError(nameof(model.MinPrice), "Minimum price cannot be less than 0.");
        }

        if (model.MinPrice.HasValue && model.MaxPrice.HasValue && model.MaxPrice.Value < model.MinPrice.Value)
        {
            ModelState.AddModelError(nameof(model.MaxPrice), "Maximum price cannot be less than minimum price.");
        }

        // 3. High-Frequency Cache: Retrieve routes with in-memory caching
        var routes = await cache.GetOrCreateAsync(CacheKeyRoutes, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.SlidingExpiration = TimeSpan.FromMinutes(3);
            return await context.Routes
                .AsNoTracking()
                .Where(r => r.IsActive)
                .ToListAsync();
        }) ?? new List<TransportRoute>();

        model.Origins = routes
            .Select(r => r.Origin)
            .Distinct()
            .OrderBy(o => o)
            .ToList();

        model.Destinations = routes
            .Select(r => r.Destination)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        // 4. High-Frequency Cache: Retrieve vehicle types with in-memory caching
        model.VehicleTypes = await cache.GetOrCreateAsync(CacheKeyVehicleTypes, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.SlidingExpiration = TimeSpan.FromMinutes(3);
            return await context.Vehicles
                .AsNoTracking()
                .Where(v => v.IsActive)
                .Select(v => v.VehicleType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }) ?? new List<string>();

        // 5. Construct base query for trip departures
        var query = context.Trips
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Reviews.Where(r => r.IsVisible))
            .Where(t => t.IsActive && t.Route!.IsActive && t.Vehicle!.IsActive);

        // 6. Apply search and filtering criteria
        if (!string.IsNullOrWhiteSpace(model.Origin))
        {
            query = query.Where(t => t.Route!.Origin == model.Origin);
        }

        if (!string.IsNullOrWhiteSpace(model.Destination))
        {
            query = query.Where(t => t.Route!.Destination == model.Destination);
        }

        if (model.DepartureDate.HasValue)
        {
            var targetDate = model.DepartureDate.Value.Date;
            var nextDay = targetDate.AddDays(1);
            query = query.Where(t => t.DepartureTime >= targetDate && t.DepartureTime < nextDay);
        }

        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var search = model.Search.Trim().ToLower();
            query = query.Where(t =>
                t.Route!.Origin.ToLower().Contains(search) ||
                t.Route.Destination.ToLower().Contains(search) ||
                t.Vehicle!.VehicleModel.ToLower().Contains(search) ||
                t.Vehicle.VehicleType.ToLower().Contains(search));
        }

        if (ModelState.IsValid)
        {
            if (model.MinPrice.HasValue)
            {
                query = query.Where(t => t.BaseFare >= model.MinPrice.Value);
            }

            if (model.MaxPrice.HasValue)
            {
                query = query.Where(t => t.BaseFare <= model.MaxPrice.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(model.VehicleType))
        {
            query = query.Where(t => t.Vehicle!.VehicleType == model.VehicleType);
        }

        if (model.OnlyAvailableSeats)
        {
            query = query.Where(t => t.AvailableSeats > 0);
        }

        if (model.DepartureTimeFrom.HasValue || model.DepartureTimeTo.HasValue)
        {
            var timeFrom = model.DepartureTimeFrom ?? TimeOnly.MinValue;
            var timeTo = model.DepartureTimeTo ?? TimeOnly.MaxValue;

            query = query.Where(t =>
                t.DepartureTime.TimeOfDay >= timeFrom.ToTimeSpan() &&
                t.DepartureTime.TimeOfDay <= timeTo.ToTimeSpan());
        }

        // 7. Pagination and sorting
        model.TotalCount = await query.CountAsync();

        query = model.SortBy switch
        {
            "price-high" => query.OrderByDescending(t => t.BaseFare),
            "price-low" => query.OrderBy(t => t.BaseFare),
            "rating" => query.OrderByDescending(t =>
                t.Reviews.Any() ? t.Reviews.Average(r => r.Rating) : 0),
            "duration" => query.OrderBy(t =>
                EF.Functions.DateDiffSecond(t.DepartureTime, t.ArrivalTime)),
            _ => query.OrderBy(t => t.DepartureTime)
        };

        var trips = await query
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();

        model.Trips = trips;

        return View(model);
    }

    // =========================================================================
    // Trip Details & Seat Pre-check (Details)
    // =========================================================================
    public async Task<IActionResult> Details(int id)
    {
        var trip = await context.Trips
            .AsNoTracking()
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Seats)
            .Include(t => t.Reviews.Where(r => r.IsVisible))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(t => t.TripId == id);

        if (trip == null)
            return NotFound();

        return View(trip);
    }

    // =========================================================================
    // Seat Selection & Booking Form (GET)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Book(int id)
    {
        var trip = await context.Trips
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Seats)
            .FirstOrDefaultAsync(t => t.TripId == id);

        if (trip == null || !trip.IsActive)
            return NotFound("Trip not found or no longer active.");

        // Automatically generate visual seats if not initialized
        if (!trip.Seats.Any())
        {
            var seats = new List<Seat>();
            int capacity = trip.TotalSeats > 0 ? trip.TotalSeats : 12;
            int bookedCount = Math.Max(0, capacity - trip.AvailableSeats);

            for (int i = 1; i <= capacity; i++)
            {
                int row = (i - 1) / 3 + 1;
                char col = (char)('A' + ((i - 1) % 3));
                string seatNum = $"{row}{col}";
                bool isBooked = i <= bookedCount;

                seats.Add(new Seat
                {
                    TripId = trip.TripId,
                    SeatNumber = seatNum,
                    SeatClass = i <= 3 ? "VIP" : "Standard",
                    SeatType = ((i - 1) % 3 == 0) ? "Window" : "Aisle",
                    IsAvailable = !isBooked,
                    Status = isBooked ? "Booked" : "Available",
                    CreatedAt = DateTime.UtcNow
                });
            }
            context.Seats.AddRange(seats);
            await context.SaveChangesAsync();
            trip.Seats = seats;
        }

        decimal unitPrice = trip.BaseFare * (1 - trip.DiscountPercentage / 100);

        var viewModel = new TransportationBookingViewModel
        {
            TripId = trip.TripId,
            Trip = trip,
            Seats = trip.Seats.OrderBy(s => s.SeatId).ToList(),
            BaseFarePerSeat = unitPrice,
            TotalAmount = unitPrice
        };

        // Automatically populate contact details from logged-in user profile
        var identityName = User.Identity?.Name;
        ApplicationUser? loggedUser = null;
        if (!string.IsNullOrEmpty(identityName))
        {
            loggedUser = await context.Users.FirstOrDefaultAsync(u =>
                (u.FirstName + " " + u.LastName).Trim() == identityName ||
                u.FirstName == identityName ||
                u.Email == identityName);
        }
        loggedUser ??= await context.Users.FirstOrDefaultAsync(u => u.AccountStatus == "Active")
                       ?? await context.Users.FirstOrDefaultAsync();

        if (loggedUser != null)
        {
            viewModel.ContactName = $"{loggedUser.FirstName} {loggedUser.LastName}".Trim();
            viewModel.ContactEmail = loggedUser.Email;
            viewModel.ContactPhone = loggedUser.PhoneNumber ?? string.Empty;
        }

        return View(viewModel);
    }

    // =========================================================================
    // Core Module 2: Concurrency-Safe Seat Reservation (POST)
    // Uses Serializable Transaction to completely prevent seat collisions/overbooking
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(TransportationBookingViewModel model)
    {
        var trip = await context.Trips
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Seats)
            .FirstOrDefaultAsync(t => t.TripId == model.TripId);

        if (trip == null)
            return NotFound();

        if (model.SelectedSeatNumbers == null || !model.SelectedSeatNumbers.Any())
        {
            ModelState.AddModelError("", "Please select at least one seat on the seat map.");
        }

        // Maximum seat limit per booking (Max 5 seats)
        if (model.SelectedSeatNumbers != null && model.SelectedSeatNumbers.Count > 5)
        {
            ModelState.AddModelError("", "You can select a maximum of 5 seats per reservation.");
        }

        // Authenticate and match the current logged-in user
        ApplicationUser? currentUser = null;
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var userGuid))
        {
            currentUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == userGuid);
        }

        if (currentUser == null)
        {
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(emailClaim))
            {
                currentUser = await context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);
            }
        }

        if (currentUser == null && !string.IsNullOrWhiteSpace(model.ContactEmail))
        {
            currentUser = await context.Users.FirstOrDefaultAsync(u => u.Email == model.ContactEmail.Trim());
        }

        currentUser ??= await context.Users.FirstOrDefaultAsync(u => u.AccountStatus == "Active")
                       ?? await context.Users.FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        model.ContactName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
        if (string.IsNullOrEmpty(model.ContactName)) model.ContactName = currentUser.FirstName;
        model.ContactEmail = currentUser.Email;

        // Server-side passenger details validation
        for (int i = 0; i < model.Passengers.Count; i++)
        {
            var p = model.Passengers[i];

            if (string.IsNullOrWhiteSpace(p.FullName) || p.FullName.Trim().Length < 2)
            {
                ModelState.AddModelError("", $"Passenger {i + 1} (Seat {p.SeatNumber}): Full name is required (minimum 2 letters).");
            }
            else if (!Regex.IsMatch(p.FullName.Trim(), @"^[A-Za-zÀ-ÿ\s'-]{2,80}$"))
            {
                ModelState.AddModelError("", $"Passenger {i + 1} (Seat {p.SeatNumber}): Full name must contain letters and spaces only.");
            }

            if (string.IsNullOrWhiteSpace(p.IdNumber) || p.IdNumber.Trim().Length < 6)
            {
                ModelState.AddModelError("", $"Passenger {i + 1} (Seat {p.SeatNumber}): Valid IC or Passport number is required (minimum 6 characters).");
            }
            else if (!Regex.IsMatch(p.IdNumber.Trim(), @"^[A-Za-z0-9-]{6,20}$"))
            {
                ModelState.AddModelError("", $"Passenger {i + 1} (Seat {p.SeatNumber}): IC/Passport must contain valid alphanumeric characters or hyphens only.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.Trip = trip;
            model.Seats = trip.Seats.OrderBy(s => s.SeatId).ToList();
            model.BaseFarePerSeat = trip.BaseFare * (1 - trip.DiscountPercentage / 100);
            return View(model);
        }

        // 1. Begin atomic serializable transaction for high concurrency protection
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            // 2. Query target seats directly within the transaction to acquire row locks
            var selectedSeats = await context.Seats
                .Where(s => s.TripId == trip.TripId && model.SelectedSeatNumbers!.Contains(s.SeatNumber))
                .ToListAsync();

            // 3. Concurrency Check: Detect if any seat was booked by another concurrent passenger
            var conflictedSeats = selectedSeats
                .Where(s => !s.IsAvailable || s.Status != "Available")
                .Select(s => s.SeatNumber)
                .ToList();

            if (conflictedSeats.Any())
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Seat collision: Seat(s) {string.Join(", ", conflictedSeats)} have just been reserved by another passenger. Please choose other seats.");

                model.Trip = trip;
                model.Seats = trip.Seats.OrderBy(s => s.SeatId).ToList();
                model.BaseFarePerSeat = trip.BaseFare * (1 - trip.DiscountPercentage / 100);
                return View(model);
            }

            // 4. Inventory check: Verify enough available seats remain
            if (trip.AvailableSeats < selectedSeats.Count)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Not enough available seats left on this trip.");

                model.Trip = trip;
                model.Seats = trip.Seats.OrderBy(s => s.SeatId).ToList();
                model.BaseFarePerSeat = trip.BaseFare * (1 - trip.DiscountPercentage / 100);
                return View(model);
            }

            // 5. Fee calculations
            decimal seatPrice = trip.BaseFare * (1 - trip.DiscountPercentage / 100);
            decimal baseTotal = seatPrice * selectedSeats.Count;
            decimal baggageTotal = model.Passengers.Sum(p => p.BaggagePrice);
            decimal insuranceTotal = model.Passengers.Sum(p => p.HasInsurance ? 5.00m : 0m);

            decimal discount = 0;
            if (!string.IsNullOrWhiteSpace(model.PromoCode))
            {
                var code = model.PromoCode.Trim().ToUpper();
                if (code == "TRAVEL2026") discount = Math.Round(baseTotal * 0.15m, 2);
                else if (code == "PROMO10") discount = Math.Min(baseTotal, 10.00m);
            }

            decimal grandTotal = Math.Max(0, baseTotal + baggageTotal + insuranceTotal - discount);

            // 6. Create booking record
            var booking = new TransportationBooking
            {
                BookingReference = "TB-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..5].ToUpper(),
                TripId = trip.TripId,
                UserId = currentUser.UserId,
                ContactName = model.ContactName,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone,
                BaseFareTotal = baseTotal,
                BaggageFeeTotal = baggageTotal,
                InsuranceFeeTotal = insuranceTotal,
                PromoCode = model.PromoCode,
                DiscountAmount = discount,
                TotalAmount = grandTotal,
                BookingStatus = "Confirmed",
                PaymentStatus = "Paid",
                PaymentMethod = model.PaymentMethod,
                BookingDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var pInput in model.Passengers)
            {
                var targetSeat = selectedSeats.FirstOrDefault(s => s.SeatNumber == pInput.SeatNumber);
                booking.Passengers.Add(new TransportationPassenger
                {
                    FullName = pInput.FullName.Trim(),
                    IdNumber = pInput.IdNumber.Trim().ToUpper(),
                    PassengerType = pInput.PassengerType ?? "Adult",
                    SeatId = targetSeat?.SeatId,
                    SeatNumber = pInput.SeatNumber,
                    BaggageOption = pInput.BaggageOption ?? "Standard (20kg Included)",
                    BaggagePrice = pInput.BaggagePrice,
                    HasTravelInsurance = pInput.HasInsurance,
                    InsurancePrice = pInput.HasInsurance ? 5.00m : 0m,
                    SpecialRequests = pInput.SpecialRequests
                });
            }

            // 7. Update physical seat states and decrement inventory inside transaction
            foreach (var seat in selectedSeats)
            {
                seat.IsAvailable = false;
                seat.Status = "Booked";
            }

            trip.AvailableSeats = Math.Max(0, trip.AvailableSeats - selectedSeats.Count);

            context.TransportationBookings.Add(booking);
            await context.SaveChangesAsync();

            // 8. Commit atomic transaction
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Confirmation), new { id = booking.BookingId });
        }
        catch
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "A concurrency conflict occurred while locking your seats. Please select your seats again.");

            model.Trip = trip;
            model.Seats = trip.Seats.OrderBy(s => s.SeatId).ToList();
            model.BaseFarePerSeat = trip.BaseFare * (1 - trip.DiscountPercentage / 100);
            return View(model);
        }
    }

    // =========================================================================
    // E-Ticket & Boarding Pass Confirmation (GET)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var booking = await context.TransportationBookings
            .AsNoTracking()
            .Include(b => b.Trip).ThenInclude(t => t!.Route)
            .Include(b => b.Trip).ThenInclude(t => t!.Vehicle)
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null)
            return NotFound("Booking reference not found.");

        return View(booking);
    }

    // =========================================================================
    // Passenger My Bookings Roster (GET)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> MyBookings()
    {
        var identityName = User.Identity?.Name;
        ApplicationUser? currentUser = null;

        if (!string.IsNullOrEmpty(identityName))
        {
            currentUser = await context.Users.FirstOrDefaultAsync(u =>
                (u.FirstName + " " + u.LastName).Trim() == identityName ||
                u.FirstName == identityName ||
                u.Email == identityName);
        }

        currentUser ??= await context.Users.FirstOrDefaultAsync(u => u.AccountStatus == "Active")
                       ?? await context.Users.FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var bookings = await context.TransportationBookings
            .AsNoTracking()
            .Include(b => b.Trip).ThenInclude(t => t!.Route)
            .Include(b => b.Trip).ThenInclude(t => t!.Vehicle)
            .Include(b => b.Passengers)
            .Where(b => b.UserId == currentUser.UserId
                     || b.ContactEmail == currentUser.Email
                     || b.ContactName == identityName)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return View(bookings);
    }

    // =========================================================================
    // Cancel Booking with Mandatory Reason (POST)
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(int id, string cancellationReason)
    {
        if (string.IsNullOrWhiteSpace(cancellationReason) || cancellationReason.Trim().Length < 5)
        {
            TempData["ErrorMessage"] = "Please provide a valid cancellation reason (minimum 5 characters).";
            return RedirectToAction(nameof(MyBookings));
        }

        var booking = await context.TransportationBookings
            .Include(b => b.Trip).ThenInclude(t => t!.Seats)
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null)
            return NotFound("Booking not found.");

        if (booking.BookingStatus == "Cancelled")
        {
            TempData["ErrorMessage"] = "This booking has already been cancelled.";
            return RedirectToAction(nameof(MyBookings));
        }

        booking.BookingStatus = "Cancelled";
        booking.PaymentStatus = "Refunded";
        booking.CancellationReason = cancellationReason.Trim();
        booking.CancelledAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

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
        TempData["SuccessMessage"] = "Your booking was successfully cancelled. Seats have been released and refund has been initiated.";

        return RedirectToAction(nameof(MyBookings));
    }

    // =========================================================================
    // Submit Trip Review & Star Rating (POST)
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(TransportationReviewViewModel model)
    {
        var trip = await context.Trips.FindAsync(model.TripId);
        if (trip == null) return NotFound("Trip not found.");

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please provide a valid rating (1-5 stars) and comment.";
            return RedirectToAction(nameof(Details), new { id = model.TripId });
        }

        var identityName = User.Identity?.Name;
        ApplicationUser? currentUser = null;
        if (!string.IsNullOrEmpty(identityName))
        {
            currentUser = await context.Users.FirstOrDefaultAsync(u =>
                (u.FirstName + " " + u.LastName).Trim() == identityName ||
                u.FirstName == identityName ||
                u.Email == identityName);
        }
        currentUser ??= await context.Users.FirstOrDefaultAsync(u => u.AccountStatus == "Active")
                       ?? await context.Users.FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var review = new TransportationReview
        {
            TripId = model.TripId,
            UserId = currentUser.UserId,
            Rating = Math.Clamp(model.Rating, 1, 5),
            Title = string.IsNullOrWhiteSpace(model.Title) ? "Great Experience" : model.Title.Trim(),
            Comment = model.Comment.Trim(),
            IsVisible = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.TransportationReviews.Add(review);
        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you! Your review has been submitted successfully.";
        return RedirectToAction(nameof(Details), new { id = model.TripId });
    }
}