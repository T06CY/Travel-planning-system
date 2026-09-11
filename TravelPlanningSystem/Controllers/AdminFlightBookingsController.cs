using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminFlightBookingsController(AppDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Weather(int page = 1, string? sort = "departure", FlightStatus? status = null)
    {
        const int pageSize = 30;
        var flightQuery = context.Flights.AsNoTracking().Include(f => f.Airline)
            .Where(f => f.DepartureTime >= DateTime.Today.AddDays(-1));
        if (status.HasValue)
            flightQuery = flightQuery.Where(f => f.Status == status.Value);
        flightQuery = sort switch
        {
            "latest" => flightQuery.OrderByDescending(f => f.DepartureTime),
            "status" => flightQuery.OrderBy(f => f.Status).ThenBy(f => f.DepartureTime),
            _ => flightQuery.OrderBy(f => f.DepartureTime)
        };
        var totalRecords = await flightQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var flights = await flightQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.Sort = sort;
        ViewBag.Status = status;
        var ids = flights.Select(f => f.FlightId).ToList();
        ViewBag.DisruptionMessages = await context.FlightBookings.AsNoTracking()
            .Where(b => b.DisruptionMessage != null && b.Segments.Any(s => ids.Contains(s.FlightId)))
            .SelectMany(b => b.Segments.Where(s => ids.Contains(s.FlightId)).Select(s => new { s.FlightId, b.DisruptionMessage }))
            .GroupBy(x => x.FlightId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.DisruptionMessage).FirstOrDefault());
        return View(flights);
    }

    [HttpGet]
    public async Task<IActionResult> Disrupt(int flightId)
    {
        var flight = await context.Flights.AsNoTracking().Include(f => f.Airline).FirstOrDefaultAsync(f => f.FlightId == flightId);
        return flight is null ? NotFound() : View(flight);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Disrupt(int flightId, FlightStatus status, string? message, string? delayCategory)
    {
        var flight = await context.Flights.FindAsync(flightId);
        if (flight is null) return NotFound();
        message = string.IsNullOrWhiteSpace(message) ? "Your flight schedule has been updated. Please check the latest details." : message.Trim();
        if (status == FlightStatus.Delayed && !string.IsNullOrWhiteSpace(delayCategory))
            message = $"Delay category: {delayCategory.Trim()}. {message}";
        if (message.Length > 500) message = message[..500];
        flight.Status = status;
        flight.IsActive = status != FlightStatus.Cancelled;
        var bookings = await context.FlightBookings.Include(b => b.Passengers).Include(b => b.Segments).Where(b => b.Segments.Any(s => s.FlightId == flightId)).ToListAsync();
        foreach (var booking in bookings)
        {
            booking.DisruptionMessage = message;
            if (status == FlightStatus.Cancelled && booking.Status != FlightBookingStatus.Cancelled)
            {
                booking.Status = FlightBookingStatus.Cancelled;
                booking.PaymentStatus = "Refunded";
                booking.CancelledAt = DateTime.UtcNow;
                booking.CancellationReason = message;
                foreach (var segment in booking.Segments.Where(s => s.FlightId == flightId))
                    flight.AvailableSeats = Math.Min(flight.SeatCapacity, flight.AvailableSeats + booking.Passengers.Count);
            }
        }
        await context.SaveChangesAsync();
        TempData["Message"] = status == FlightStatus.Cancelled ? $"Flight cancelled. {bookings.Count} booking(s) marked for full refund and notified." : $"Flight marked {status}. A message was saved for affected passengers.";
        return RedirectToAction(nameof(Weather));
    }

    public async Task<IActionResult> Index(string? search, FlightBookingStatus? status, int page = 1)
    {
        const int pageSize = 30;
        page = Math.Max(1, page);
        var query = context.FlightBookings
            .AsNoTracking()
            .Include(b => b.Passengers)
            .Include(b => b.Segments)
                .ThenInclude(s => s.Flight)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b =>
                b.BookingReference.Contains(search) ||
                b.ContactName.Contains(search) ||
                b.ContactEmail.Contains(search));
        }
        if (status.HasValue) query = query.Where(b => b.Status == status);

        ViewBag.Search = search;
        ViewBag.Status = status;
        var totalRecords = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
        page = Math.Min(page, totalPages);
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = totalRecords;
        return View(await query.OrderByDescending(b => b.BookingDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var booking = await context.FlightBookings
            .AsNoTracking()
            .AsSplitQuery()
            .Include(b => b.Passengers)
            .Include(b => b.Segments.OrderBy(s => s.SegmentOrder))
                .ThenInclude(s => s.Flight)
                    .ThenInclude(f => f!.Airline)
            .FirstOrDefaultAsync(b => b.FlightBookingId == id);
        if (booking is null) return NotFound();

        var segmentIds = booking.Segments.OrderBy(s => s.SegmentOrder).ToList();
        var flightIds = segmentIds.Select(s => s.FlightId).Distinct().ToList();
        var reservedPassengers = await context.FlightPassengers
            .AsNoTracking()
            .Include(p => p.FlightBooking)
                .ThenInclude(b => b!.Segments)
            .Where(p => p.FlightBooking != null &&
                        p.FlightBooking.Status != FlightBookingStatus.Cancelled &&
                        p.FlightBooking.Segments.Any(s => flightIds.Contains(s.FlightId)))
            .ToListAsync();

        var outboundFlightId = segmentIds.FirstOrDefault()?.FlightId;
        var returnFlightId = segmentIds.Skip(1).FirstOrDefault()?.FlightId;
        ViewBag.OccupiedOutboundSeats = reservedPassengers
            .Where(p => p.FlightBooking!.Segments.Any(s => s.FlightId == outboundFlightId))
            .Select(p => p.SeatNumber)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        ViewBag.OccupiedReturnSeats = reservedPassengers
            .Where(p => returnFlightId.HasValue && p.FlightBooking!.Segments.Any(s => s.FlightId == returnFlightId.Value))
            .Select(p => p.ReturnSeatNumber)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return View(booking);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSeat(int id, int passengerId, string seatNumber, string? returnSeatNumber)
    {
        var booking = await context.FlightBookings
            .Include(b => b.Passengers)
            .Include(b => b.Segments)
                .ThenInclude(s => s.Flight)
            .FirstOrDefaultAsync(b => b.FlightBookingId == id);

        if (booking is null) return NotFound();

        var passenger = booking.Passengers.FirstOrDefault(p => p.FlightPassengerId == passengerId);
        var orderedSegments = booking.Segments.OrderBy(s => s.SegmentOrder).ToList();
        var outboundSegment = orderedSegments.FirstOrDefault();
        var returnSegment = orderedSegments.Skip(1).FirstOrDefault();
        var aircraft = outboundSegment?.Flight;
        var bookingFlightIds = orderedSegments.Select(s => s.FlightId).ToList();
        seatNumber = (seatNumber ?? string.Empty).Trim().ToUpperInvariant();
        returnSeatNumber = (returnSeatNumber ?? string.Empty).Trim().ToUpperInvariant();
        var isReturnTrip = string.Equals(booking.TripType, "Return", StringComparison.OrdinalIgnoreCase) && returnSegment?.Flight is not null;

        if (passenger is null || aircraft is null)
        {
            TempData["Error"] = "Passenger or flight could not be found.";
        }
        else if (!IsValidSeat(seatNumber, aircraft.SeatCapacity) ||
            !FlightFareRules.IsSeatInCabin(seatNumber, aircraft.SeatCapacity, outboundSegment?.CabinClass) ||
            (isReturnTrip && (!IsValidSeat(returnSeatNumber, returnSegment!.Flight!.SeatCapacity) ||
                !FlightFareRules.IsSeatInCabin(returnSeatNumber, returnSegment.Flight.SeatCapacity, returnSegment.CabinClass))))
        {
            TempData["Error"] = "One or more seats do not match the passenger's booked cabin or aircraft seat map.";
        }
        else
        {
            var alreadyUsed = await context.FlightPassengers
                .AnyAsync(p => p.FlightPassengerId != passengerId &&
                    p.SeatNumber == seatNumber &&
                    p.FlightBooking != null &&
                    p.FlightBooking.Status != FlightBookingStatus.Cancelled &&
                    p.FlightBooking.Segments.Any(s => s.FlightId == bookingFlightIds[0]));

            var returnAlreadyUsed = isReturnTrip && await context.FlightPassengers.AnyAsync(p =>
                p.FlightPassengerId != passengerId && p.ReturnSeatNumber == returnSeatNumber &&
                p.FlightBooking != null && p.FlightBooking.Status != FlightBookingStatus.Cancelled &&
                p.FlightBooking.Segments.Any(s => s.FlightId == bookingFlightIds[1]));

            var usedByThisBooking = booking.Passengers.Any(p =>
                p.FlightPassengerId != passengerId && p.SeatNumber == seatNumber);

            var returnUsedByThisBooking = isReturnTrip && booking.Passengers.Any(p => p.FlightPassengerId != passengerId && p.ReturnSeatNumber == returnSeatNumber);
            if (alreadyUsed || usedByThisBooking || returnAlreadyUsed || returnUsedByThisBooking)
                TempData["Error"] = $"Seat {seatNumber} is already occupied.";
            else
            {
                passenger.SeatNumber = seatNumber;
                passenger.ReturnSeatNumber = isReturnTrip ? returnSeatNumber : null;
                await context.SaveChangesAsync();
                TempData["Message"] = $"Seat {seatNumber} assigned to {passenger.FirstName} {passenger.LastName}.";
            }
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static bool IsValidSeat(string seatNumber, int seatCapacity)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                seatNumber, @"^[1-9][0-9]{0,2}[A-F]$",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant))
            return false;

        var row = int.Parse(seatNumber[..^1]);
        var column = seatNumber[^1] - 'A';
        return ((row - 1) * 6) + column + 1 <= seatCapacity;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, FlightBookingStatus status)
    {
        var booking = await context.FlightBookings
            .Include(b => b.Passengers)
            .Include(b => b.Segments)
                .ThenInclude(s => s.Flight)
            .FirstOrDefaultAsync(b => b.FlightBookingId == id);
        if (booking is null) return NotFound();

        var passengerCount = booking.Passengers.Count;
        if (booking.Status != FlightBookingStatus.Cancelled &&
            status == FlightBookingStatus.Cancelled)
        {
            foreach (var segment in booking.Segments.Where(s => s.Flight != null))
            {
                segment.Flight!.AvailableSeats = Math.Min(
                    segment.Flight.SeatCapacity,
                    segment.Flight.AvailableSeats + passengerCount);
            }
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancellationReason = "Cancelled by administrator";
        }
        else if (booking.Status == FlightBookingStatus.Cancelled &&
            status != FlightBookingStatus.Cancelled)
        {
            if (booking.Segments.Any(s => s.Flight == null ||
                s.Flight.AvailableSeats < passengerCount))
            {
                TempData["Error"] = "The booking cannot be restored because seats are unavailable.";
                return RedirectToAction(nameof(Details), new { id });
            }
            foreach (var segment in booking.Segments.Where(s => s.Flight != null))
                segment.Flight!.AvailableSeats -= passengerCount;
            booking.CancelledAt = null;
            booking.CancellationReason = null;
        }

        booking.Status = status;
        await context.SaveChangesAsync();
        TempData["Message"] = $"Booking status changed to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
