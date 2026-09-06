using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

public class HotelReservationsController(AppDbContext context) : Controller
{
    private int CurrentUserId => 1;

    [HttpGet]
    public async Task<IActionResult> Create(int roomId, DateTime? checkIn, DateTime? checkOut)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = Url.Action(nameof(Create), "HotelReservations", new { roomId, checkIn, checkOut });
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        var room = await context.HotelRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.HotelRoomId == roomId && r.IsActive);

        if (room is null)
            return NotFound();

        return View(new HotelReservationViewModel
        {
            HotelRoomId = roomId,
            HotelName = room.HotelName,
            RoomName = room.RoomName,
            PricePerNight = room.PricePerNight,
            Capacity = room.Capacity,
            CheckInDate = checkIn?.Date >= DateTime.Today
                ? checkIn.Value.Date
                : DateTime.Today.AddDays(1),
            CheckOutDate = checkOut?.Date >= DateTime.Today
                ? checkOut.Value.Date
                : DateTime.Today.AddDays(2)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HotelReservationViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = Url.Action(nameof(Create), "HotelReservations", new
            {
                roomId = model.HotelRoomId,
                checkIn = model.CheckInDate,
                checkOut = model.CheckOutDate
            });

            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        var room = await context.HotelRooms
            .FirstOrDefaultAsync(r => r.HotelRoomId == model.HotelRoomId && r.IsActive);

        if (room is null)
            return NotFound();

        model.HotelName = room.HotelName;
        model.RoomName = room.RoomName;
        model.PricePerNight = room.PricePerNight;
        model.Capacity = room.Capacity;

        var checkInAt = model.CheckInDate.Date.Add(model.CheckInTime);
        var checkOutAt = model.CheckOutDate.Date.Add(model.CheckOutTime);

        if (model.CheckInDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(model.CheckInDate),
                "Check-in date cannot be in the past.");
        }

        if (checkOutAt <= checkInAt)
        {
            ModelState.AddModelError(
                nameof(model.CheckOutDate),
                "Check-out date and time must be after check-in date and time.");
        }

        if (model.GuestCount <= 0)
        {
            ModelState.AddModelError(
                nameof(model.GuestCount),
                "Guest count must be at least 1.");
        }

        if (model.GuestCount > room.Capacity)
        {
            ModelState.AddModelError(
                nameof(model.GuestCount),
                $"This room allows up to {room.Capacity} guest(s).");
        }

        // First let SQL Server filter by date only. Then perform the exact
        // DateTime + TimeSpan overlap check in memory because EF Core cannot
        // translate DateTime.Add(TimeSpan) to SQL Server.
        var possibleReservations = await context.HotelReservations
            .AsNoTracking()
            .Where(b =>
                b.HotelRoomId == room.HotelRoomId &&
                b.ReservationStatus != HotelReservationStatus.Cancelled &&
                b.CheckInDate <= model.CheckOutDate.Date &&
                b.CheckOutDate >= model.CheckInDate.Date)
            .ToListAsync();

        var occupied = possibleReservations.Count(b =>
        {
            var existingCheckIn = b.CheckInDate.Date.Add(b.CheckInTime);
            var existingCheckOut = b.CheckOutDate.Date.Add(b.CheckOutTime);

            return existingCheckIn < checkOutAt && existingCheckOut > checkInAt;
        });

        if (occupied >= room.TotalRooms)
        {
            ModelState.AddModelError(
                string.Empty,
                "This room is no longer available for the selected date and time.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var nights = Math.Max(1, (int)Math.Ceiling((checkOutAt - checkInAt).TotalDays));

        var reservation = new HotelReservation
        {
            ReservationReference =
                $"HTL{DateTime.Now:yyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            UserId = CurrentUserId,
            HotelRoomId = room.HotelRoomId,
            CheckInDate = model.CheckInDate.Date,
            CheckOutDate = model.CheckOutDate.Date,
            CheckInTime = model.CheckInTime,
            CheckOutTime = model.CheckOutTime,
            GuestCount = model.GuestCount,
            PricePerNight = room.PricePerNight,
            TotalAmount = room.PricePerNight * nights,
            ContactName = model.ContactName.Trim(),
            ContactEmail = model.ContactEmail.Trim(),
            ContactPhone = model.ContactPhone.Trim()
        };

        context.HotelReservations.Add(reservation);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = reservation.HotelReservationId });
    }

    public async Task<IActionResult> MyReservations(string? status)
    {
        var query = context.HotelReservations
            .AsNoTracking()
            .Include(b => b.HotelRoom)
            .ThenInclude(r => r!.Photos)
            .Where(b => b.UserId == CurrentUserId);

        if (Enum.TryParse<HotelReservationStatus>(status, true, out var parsed))
            query = query.Where(b => b.ReservationStatus == parsed);

        ViewBag.Status = status;

        return View(await query
            .OrderByDescending(b => b.ReservationDate)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
        => await Owned(id) is { } reservation ? View(reservation) : NotFound();

    [HttpGet]
    public async Task<IActionResult> Cancel(int id)
        => await Owned(id) is { ReservationStatus: HotelReservationStatus.Confirmed } booking &&
           booking.CheckInDate > DateTime.Today
            ? View(booking)
            : BadRequest();

    [HttpPost]
    [ActionName("Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelConfirmed(int id, string cancellationReason)
    {
        var booking = await context.HotelReservations
            .FirstOrDefaultAsync(b => b.HotelReservationId == id && b.UserId == CurrentUserId);

        if (booking is null ||
            booking.ReservationStatus != HotelReservationStatus.Confirmed ||
            booking.CheckInDate <= DateTime.Today)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(cancellationReason) || cancellationReason.Length > 400)
        {
            ModelState.AddModelError(
                "cancellationReason",
                "Please provide a cancellation reason (maximum 400 characters).");

            return View("Cancel", booking);
        }

        booking.ReservationStatus = HotelReservationStatus.Cancelled;
        booking.CancellationReason = cancellationReason.Trim();
        booking.CancelledAt = DateTime.Now;

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Review(int reservationId)
    {
        var booking = await Owned(reservationId);

        if (booking is null || booking.ReservationStatus != HotelReservationStatus.Completed)
            return BadRequest();

        return View(new HotelReviewViewModel
        {
            HotelReservationId = reservationId,
            RoomName = booking.HotelRoom!.RoomName,
            Rating = booking.Review?.Rating ?? 5,
            Comment = booking.Review?.Comment ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(HotelReviewViewModel model)
    {
        var booking = await context.HotelReservations
            .Include(b => b.Review)
            .Include(b => b.HotelRoom)
            .FirstOrDefaultAsync(b =>
                b.HotelReservationId == model.HotelReservationId &&
                b.UserId == CurrentUserId);

        if (booking?.HotelRoom is null ||
            booking.ReservationStatus != HotelReservationStatus.Completed)
        {
            return BadRequest();
        }

        model.RoomName = booking.HotelRoom.RoomName;

        if (!ModelState.IsValid)
            return View(model);

        if (booking.Review is null)
        {
            context.HotelReviews.Add(new HotelReview
            {
                HotelRoomId = booking.HotelRoomId,
                HotelReservationId = booking.HotelReservationId,
                UserId = CurrentUserId,
                Rating = model.Rating,
                Comment = model.Comment.Trim()
            });
        }
        else
        {
            booking.Review.Rating = model.Rating;
            booking.Review.Comment = model.Comment.Trim();
        }

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = booking.HotelReservationId });
    }

    private Task<HotelReservation?> Owned(int id)
        => context.HotelReservations
            .AsNoTracking()
            .Include(b => b.HotelRoom)
            .ThenInclude(r => r!.Photos)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b =>
                b.HotelReservationId == id &&
                b.UserId == CurrentUserId);
}
