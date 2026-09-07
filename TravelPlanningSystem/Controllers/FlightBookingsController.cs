using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

[Authorize]
public class FlightBookingsController(AppDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(
        [FromQuery] List<int> flightIds,
        int passengers = 1,
        string tripType = "One-way")
    {
        var model = new FlightBookingCreateViewModel
        {
            FlightIds = flightIds.Distinct().Take(5).ToList(),
            PassengerCount = Math.Clamp(passengers, 1, 9),
            TripType = tripType
        };

        if (!await LoadBookingFormAsync(model))
        {
            TempData["Error"] = "Select one available flight for every journey segment.";
            return RedirectToAction("Index", "Flight");
        }

        var email = CurrentEmail();
        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);

        model.ContactEmail = email;
        model.ContactName = user is null
            ? User.Identity?.Name ?? string.Empty
            : $"{user.FirstName} {user.LastName}";
        model.ContactPhone = user?.PhoneNumber ?? string.Empty;

        for (var index = 0; index < model.PassengerCount; index++)
        {
            model.Passengers.Add(new FlightPassengerInput());
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FlightBookingCreateViewModel model)
    {
        model.FlightIds = model.FlightIds.Distinct().Take(5).ToList();
        model.PassengerCount = Math.Clamp(model.PassengerCount, 1, 9);

        if (model.Passengers.Count != model.PassengerCount)
        {
            ModelState.AddModelError(nameof(model.Passengers), "Enter details for every passenger.");
        }

        foreach (var passenger in model.Passengers)
        {
            passenger.FirstName = (passenger.FirstName ?? string.Empty).Trim();
            passenger.LastName = (passenger.LastName ?? string.Empty).Trim();
            passenger.PassportNumber = (passenger.PassportNumber ?? string.Empty).Trim().ToUpperInvariant();
            passenger.Nationality = (passenger.Nationality ?? string.Empty).Trim();

            if (IsPlaceholder(passenger.FirstName))
                ModelState.AddModelError(nameof(model.Passengers), "Enter the passenger's real first name.");
            if (IsPlaceholder(passenger.LastName))
                ModelState.AddModelError(nameof(model.Passengers), "Enter the passenger's real last name.");
            if (IsPlaceholder(passenger.PassportNumber))
                ModelState.AddModelError(nameof(model.Passengers), "Enter a valid passport or ID number.");

            if (!IsValidPassportOrId(passenger.Nationality, passenger.PassportNumber))
            {
                ModelState.AddModelError(
                    $"Passengers[{model.Passengers.IndexOf(passenger)}].PassportNumber",
                    passenger.Nationality.Equals("Malaysian", StringComparison.OrdinalIgnoreCase)
                        ? "Malaysian documents must be a 12-digit MyKad number or a passport number with 1 letter followed by 8 digits."
                        : "Use 6 to 30 letters and numbers for the passport or ID number.");
            }

            passenger.SeatNumber = (passenger.SeatNumber ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(passenger.SeatNumber))
            {
                ModelState.AddModelError(nameof(model.Passengers), "Please select a seat for every passenger.");
            }

            passenger.ReturnSeatNumber = (passenger.ReturnSeatNumber ?? string.Empty).Trim().ToUpperInvariant();
            passenger.PassengerType = (passenger.PassengerType ?? "Adult").Trim();

            if (passenger.PassengerType is not ("Adult" or "Child"))
                ModelState.AddModelError(nameof(model.Passengers), "Passenger type must be Adult or Child.");

            if (passenger.DateOfBirth.HasValue && passenger.DateOfBirth.Value.Date >= DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.Passengers), "Passenger date of birth must be before today.");
            }

            if (passenger.DateOfBirth.HasValue && passenger.PassengerType is "Adult" or "Child")
            {
                var age = CalculateAge(passenger.DateOfBirth.Value.Date, DateTime.Today);
                if (passenger.PassengerType == "Adult" && age < 12)
                    ModelState.AddModelError(nameof(model.Passengers), "Adult passengers must be at least 12 years old.");
                if (passenger.PassengerType == "Child" && (age < 2 || age > 11))
                    ModelState.AddModelError(nameof(model.Passengers), "Child passengers must be 2 to 11 years old.");
            }
        }

        if (!IsValidPhone(model.PhoneCountry, model.ContactPhone))
            ModelState.AddModelError(nameof(model.ContactPhone), PhoneFormatMessage(model.PhoneCountry));

        if (model.Passengers
            .GroupBy(p => p.SeatNumber, StringComparer.OrdinalIgnoreCase)
            .Any(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1))
        {
            ModelState.AddModelError(nameof(model.Passengers), "Each passenger must select a different seat.");
        }

        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var flights = await context.Flights
            .Include(f => f.Airline)
            .Where(f => model.FlightIds.Contains(f.FlightId))
            .ToListAsync();

        var orderedFlights = model.FlightIds
            .Select(id => flights.FirstOrDefault(f => f.FlightId == id))
            .Where(f => f is not null)
            .Cast<Flight>()
            .ToList();

        var isReturnTrip = string.Equals(model.TripType, "Return", StringComparison.OrdinalIgnoreCase)
            && orderedFlights.Count >= 2;

        if (isReturnTrip && model.Passengers.Any(p => string.IsNullOrWhiteSpace(p.ReturnSeatNumber)))
            ModelState.AddModelError(nameof(model.Passengers), "Please select a return seat for every passenger.");

        if (orderedFlights.Count > 0)
        {
            foreach (var passenger in model.Passengers.Where(p => !string.IsNullOrWhiteSpace(p.SeatNumber)))
            {
                if (!IsValidSeat(passenger.SeatNumber, orderedFlights[0].SeatCapacity))
                {
                    ModelState.AddModelError(nameof(model.Passengers), $"Seat {passenger.SeatNumber} is not available on this aircraft.");
                }
                if (isReturnTrip && !IsValidSeat(passenger.ReturnSeatNumber!, orderedFlights[1].SeatCapacity))
                {
                    ModelState.AddModelError(nameof(model.Passengers), $"Return seat {passenger.ReturnSeatNumber} is not available on this aircraft.");
                }
            }
            if (isReturnTrip && model.Passengers.GroupBy(p => p.ReturnSeatNumber, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            {
                ModelState.AddModelError(nameof(model.Passengers), "Each passenger must select a different return seat.");
            }
        }

        if (orderedFlights.Count != model.FlightIds.Count || orderedFlights.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "One or more selected flights are unavailable.");
        }

        if (orderedFlights.Any(f =>
            !f.IsActive ||
            f.DepartureTime <= DateTime.Now ||
            f.AvailableSeats < model.PassengerCount ||
            f.Status is FlightStatus.Cancelled or FlightStatus.Departed or FlightStatus.Arrived))
        {
            ModelState.AddModelError(string.Empty, "A selected flight no longer has enough available seats.");
        }

        var selectedSeats = model.Passengers
            .Select(p => p.SeatNumber)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var selectedReturnSeats = model.Passengers.Select(p => p.ReturnSeatNumber)
            .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        if (selectedSeats.Count > 0)
        {
            var occupiedSeats = await context.FlightPassengers
                .Where(p =>
                    selectedSeats.Contains(p.SeatNumber) &&
                    p.FlightBooking != null &&
                    p.FlightBooking.Status != FlightBookingStatus.Cancelled &&
                    p.FlightBooking.Segments.Any(s => model.FlightIds.Contains(s.FlightId)))
                .Select(p => p.SeatNumber)
                .Distinct()
                .ToListAsync();

            if (occupiedSeats.Count > 0)
            {
                ModelState.AddModelError(nameof(model.Passengers), $"Seat(s) {string.Join(", ", occupiedSeats)} are no longer available for one of the selected flights.");
            }
        }

        if (isReturnTrip && selectedReturnSeats.Count > 0)
        {
            var returnFlightId = orderedFlights[1].FlightId;
            var occupiedReturnSeats = await context.FlightPassengers
                .Where(p => selectedReturnSeats.Contains(p.SeatNumber) &&
                    p.FlightBooking != null && p.FlightBooking.Status != FlightBookingStatus.Cancelled &&
                    p.FlightBooking.Segments.Any(s => s.FlightId == returnFlightId))
                .Select(p => p.SeatNumber).Distinct().ToListAsync();

            if (occupiedReturnSeats.Count > 0)
            {
                ModelState.AddModelError(nameof(model.Passengers), $"Return seat(s) {string.Join(", ", occupiedReturnSeats)} are no longer available.");
            }
        }

        if (!ModelState.IsValid)
        {
            await transaction.RollbackAsync();
            await LoadBookingFormAsync(model);
            return View(model);
        }

        // ⭐ 1. 精细费用核算：机票基准票价 + 托运行李额加购 + 航空延误意外险 - 优惠券抵扣
        decimal flightBaseTotal = orderedFlights.Sum(f => f.Price) * model.PassengerCount;
        decimal baggageTotal = model.BaggagePrice * model.PassengerCount;
        decimal insuranceTotal = model.HasTravelInsurance ? (18.00m * model.PassengerCount) : 0m;
        decimal addonTotal = baggageTotal + insuranceTotal;

        // 优惠券折扣规则 (TRAVEL2026 享 85 折, FLY50 减 RM 50, PROMO10 减 RM 10)
        decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(model.PromoCode))
        {
            var code = model.PromoCode.Trim().ToUpper();
            if (code == "TRAVEL2026") discount = Math.Round(flightBaseTotal * 0.15m, 2);
            else if (code == "FLY50") discount = Math.Min(flightBaseTotal, 50.00m);
            else if (code == "PROMO10") discount = Math.Min(flightBaseTotal, 10.00m);
        }

        decimal grandTotal = Math.Max(0, flightBaseTotal + addonTotal - discount);

        // ⭐ 2. 创建并保存带完整支付、行李与优惠明细的订单
        var booking = new FlightBooking
        {
            BookingReference = CreateBookingReference(),
            UserEmail = CurrentEmail(),
            TripType = model.TripType,
            ContactName = model.ContactName.Trim(),
            ContactEmail = model.ContactEmail.Trim(),
            ContactPhone = FormatPhone(model.PhoneCountry, model.ContactPhone),
            BookingDate = DateTime.UtcNow,
            TotalAmount = grandTotal,
            AddonFee = addonTotal,
            DiscountAmount = discount,
            PromoCode = model.PromoCode,
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = "Paid",
            BaggageOption = model.BaggageOption ?? "Cabin Baggage 7kg (Free)",
            HasTravelInsurance = model.HasTravelInsurance,
            Status = FlightBookingStatus.Confirmed
        };

        for (var index = 0; index < orderedFlights.Count; index++)
        {
            var flight = orderedFlights[index];
            flight.AvailableSeats -= model.PassengerCount;

            booking.Segments.Add(new FlightBookingSegment
            {
                FlightId = flight.FlightId,
                SegmentOrder = index + 1,
                PricePerPassenger = flight.Price
            });
        }

        foreach (var passenger in model.Passengers)
        {
            booking.Passengers.Add(new FlightPassenger
            {
                FirstName = passenger.FirstName.Trim(),
                LastName = passenger.LastName.Trim(),
                PassportNumber = passenger.PassportNumber.Trim().ToUpperInvariant(),
                Nationality = passenger.Nationality.Trim(),
                DateOfBirth = passenger.DateOfBirth!.Value.Date,
                PassengerType = passenger.PassengerType,
                SeatNumber = passenger.SeatNumber,
                ReturnSeatNumber = isReturnTrip ? passenger.ReturnSeatNumber : null,
                MealPreference = passenger.MealPreference
            });
        }

        context.FlightBookings.Add(booking);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToAction(nameof(Confirmation), new { id = booking.FlightBookingId });
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var booking = await FindOwnedBookingAsync(id);
        return booking is null ? NotFound() : View(booking);
    }

    public async Task<IActionResult> Details(int id)
    {
        var booking = await FindOwnedBookingAsync(id);
        return booking is null ? NotFound() : View(booking);
    }

    public async Task<IActionResult> MyBookings()
    {
        var bookings = await context.FlightBookings
            .AsNoTracking()
            .AsSplitQuery()
            .Include(b => b.Passengers)
            .Include(b => b.Segments.OrderBy(s => s.SegmentOrder))
                .ThenInclude(s => s.Flight)
                    .ThenInclude(f => f!.Airline)
            .Where(b => b.UserEmail == CurrentEmail())
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        var now = DateTime.Now;
        var model = new FlightBookingHistoryViewModel
        {
            Cancelled = bookings
                .Where(b => b.Status == FlightBookingStatus.Cancelled)
                .ToList(),
            Past = bookings
                .Where(b => b.Status != FlightBookingStatus.Cancelled &&
                    b.Segments.All(s => s.Flight != null && s.Flight.ArrivalTime < now))
                .ToList(),
            Upcoming = bookings
                .Where(b => b.Status != FlightBookingStatus.Cancelled &&
                    b.Segments.Any(s => s.Flight != null && s.Flight.ArrivalTime >= now))
                .ToList()
        };

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string cancellationReason)
    {
        var booking = await context.FlightBookings
            .Include(b => b.Passengers)
            .Include(b => b.Segments)
                .ThenInclude(s => s.Flight)
            .FirstOrDefaultAsync(b =>
                b.FlightBookingId == id &&
                b.UserEmail == CurrentEmail());

        if (booking is null) return NotFound();

        if (booking.Status == FlightBookingStatus.Cancelled)
        {
            TempData["Error"] = "This booking is already cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (booking.Segments.Any(s => s.Flight == null || s.Flight.DepartureTime <= DateTime.Now))
        {
            TempData["Error"] = "A booking cannot be cancelled after travel has started.";
            return RedirectToAction(nameof(Details), new { id });
        }

        booking.Status = FlightBookingStatus.Cancelled;
        booking.CancellationReason = string.IsNullOrWhiteSpace(cancellationReason)
            ? "Cancelled by passenger"
            : cancellationReason.Trim();
        booking.CancelledAt = DateTime.UtcNow;

        foreach (var segment in booking.Segments.Where(s => s.Flight != null))
        {
            segment.Flight!.AvailableSeats = Math.Min(
                segment.Flight.SeatCapacity,
                segment.Flight.AvailableSeats + booking.Passengers.Count);
        }

        await context.SaveChangesAsync();
        TempData["Message"] = "Your flight booking was cancelled successfully.";
        return RedirectToAction(nameof(MyBookings));
    }

    public async Task<IActionResult> Ticket(int id)
    {
        var booking = await FindOwnedBookingAsync(id);
        if (booking is null) return NotFound();

        var route = string.Join(" | ", booking.Segments
            .OrderBy(s => s.SegmentOrder)
            .Select(s => $"{s.Flight?.From}-{s.Flight?.To}"));

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(
            $"TravelMate|{booking.BookingReference}|{booking.ContactName}|{route}",
            QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        ViewBag.QrCode = Convert.ToBase64String(qrCode.GetGraphic(8));

        return View(booking);
    }

    private async Task<bool> LoadBookingFormAsync(FlightBookingCreateViewModel model)
    {
        var flights = await context.Flights
            .AsNoTracking()
            .Include(f => f.Airline)
            .Where(f => model.FlightIds.Contains(f.FlightId))
            .ToListAsync();

        model.Flights = model.FlightIds
            .Select(id => flights.FirstOrDefault(f => f.FlightId == id))
            .Where(f => f is not null)
            .Cast<Flight>()
            .ToList();
        model.SeatCapacity = model.Flights.FirstOrDefault()?.SeatCapacity ?? 180;

        model.OccupiedSeats = await context.FlightPassengers
            .AsNoTracking()
            .Where(passenger =>
                passenger.SeatNumber != "Not Assigned" &&
                passenger.FlightBooking != null &&
                passenger.FlightBooking.Status != FlightBookingStatus.Cancelled &&
                passenger.FlightBooking.Segments.Any(segment =>
                    model.FlightIds.Contains(segment.FlightId)))
            .Select(passenger => passenger.SeatNumber)
            .Distinct()
            .ToListAsync();
        model.TotalAmount = model.Flights.Sum(f => f.Price) * model.PassengerCount;

        return model.Flights.Count == model.FlightIds.Count && model.Flights.Count > 0;
    }

    private async Task<FlightBooking?> FindOwnedBookingAsync(int id)
    {
        var query = context.FlightBookings
            .AsNoTracking()
            .AsSplitQuery()
            .Include(b => b.Passengers)
            .Include(b => b.Segments.OrderBy(s => s.SegmentOrder))
                .ThenInclude(s => s.Flight)
                    .ThenInclude(f => f!.Airline)
            .Where(b => b.FlightBookingId == id);

        if (!User.IsInRole("Administrator"))
        {
            query = query.Where(b => b.UserEmail == CurrentEmail());
        }

        return await query.FirstOrDefaultAsync();
    }

    private string CurrentEmail() =>
        User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    private static string CreateBookingReference() =>
        $"FLT{DateTime.UtcNow:yyMMdd}{Random.Shared.Next(100000, 999999)}";

    private static bool IsValidSeat(string seatNumber, int seatCapacity)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(
            seatNumber,
            @"^[1-9][0-9]{0,2}[A-F]$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant))
        {
            return false;
        }

        var row = int.Parse(seatNumber[..^1]);
        var column = seatNumber[^1] - 'A';
        return ((row - 1) * 6) + column + 1 <= seatCapacity;
    }

    private static int CalculateAge(DateTime dateOfBirth, DateTime today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }

    private static bool IsValidPhone(string country, string phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        return country == "MY"
            ? System.Text.RegularExpressions.Regex.IsMatch(digits, @"^(01[0-9]{8,9}|601[0-9]{8,9})$")
            : System.Text.RegularExpressions.Regex.IsMatch(phone ?? string.Empty, @"^\+[1-9][0-9]{7,14}$");
    }

    private static bool IsValidPassportOrId(string? nationality, string? passportOrId)
    {
        var value = (passportOrId ?? string.Empty).Trim().ToUpperInvariant();
        if (string.Equals(nationality, "Malaysian", StringComparison.OrdinalIgnoreCase))
        {
            return System.Text.RegularExpressions.Regex.IsMatch(value, @"^(\d{12}|[A-Z]\d{8})$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        }

        return System.Text.RegularExpressions.Regex.IsMatch(value, @"^[A-Z0-9]{6,30}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }

    private static string FormatPhone(string country, string phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        return country == "MY" && digits.StartsWith("0") ? "+60" + digits[1..] : (phone ?? string.Empty).Trim();
    }

    private static string PhoneFormatMessage(string country) => country == "MY"
        ? "Enter a Malaysian mobile number such as 0123456789."
        : "Enter an international number such as +6581234567.";

    private static bool IsPlaceholder(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var normalized = value.Trim().ToLowerInvariant();
        var blocked = new[] { "xxx", "test", "testing", "abc", "sample", "none", "n/a", "na", "unknown" };
        return blocked.Contains(normalized) || normalized.All(c => c == normalized[0]);
    }
}