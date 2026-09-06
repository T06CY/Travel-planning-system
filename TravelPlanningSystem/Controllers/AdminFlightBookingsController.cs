using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminFlightBookingsController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? search, FlightBookingStatus? status)
    {
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
        return View(await query.OrderByDescending(b => b.BookingDate).ToListAsync());
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
        return booking is null ? NotFound() : View(booking);
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
        var aircraft = booking.Segments.Select(s => s.Flight).FirstOrDefault(f => f is not null);
        var bookingFlightIds = booking.Segments.Select(s => s.FlightId).ToList();
        seatNumber = (seatNumber ?? string.Empty).Trim().ToUpperInvariant();
        returnSeatNumber = (returnSeatNumber ?? string.Empty).Trim().ToUpperInvariant();
        var isReturnTrip = string.Equals(booking.TripType, "Return", StringComparison.OrdinalIgnoreCase) && bookingFlightIds.Count >= 2;

        if (passenger is null || aircraft is null)
        {
            TempData["Error"] = "Passenger or flight could not be found.";
        }
        else if (!IsValidSeat(seatNumber, aircraft.SeatCapacity) ||
            (isReturnTrip && !IsValidSeat(returnSeatNumber, booking.Segments.OrderBy(s => s.SegmentOrder).Skip(1).First().Flight?.SeatCapacity ?? aircraft.SeatCapacity)))
        {
            TempData["Error"] = "One or more seat numbers are outside the aircraft seat map.";
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
