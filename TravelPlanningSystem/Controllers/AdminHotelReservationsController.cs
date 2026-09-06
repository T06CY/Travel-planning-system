using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminHotelReservationsController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var query = context.HotelReservations
            .AsNoTracking()
            .Include(r => r.HotelRoom)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.ReservationReference.Contains(term) ||
                r.ContactName.Contains(term) ||
                r.ContactEmail.Contains(term) ||
                (r.HotelRoom != null &&
                    (r.HotelRoom.HotelName.Contains(term) || r.HotelRoom.RoomName.Contains(term))));
        }

        if (Enum.TryParse<HotelReservationStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.ReservationStatus == parsedStatus);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;

        return View(await query
            .OrderByDescending(r => r.ReservationDate)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var reservation = await context.HotelReservations
            .AsNoTracking()
            .Include(r => r.HotelRoom)
                .ThenInclude(room => room!.Photos)
            .Include(r => r.Review)
            .FirstOrDefaultAsync(r => r.HotelReservationId == id);

        return reservation is null ? NotFound() : View(reservation);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, HotelReservationStatus status)
    {
        var reservation = await context.HotelReservations.FindAsync(id);
        if (reservation is null) return NotFound();

        reservation.ReservationStatus = status;

        if (status == HotelReservationStatus.Cancelled)
        {
            reservation.CancelledAt ??= DateTime.Now;
            reservation.CancellationReason ??= "Cancelled by administrator.";
        }
        else
        {
            reservation.CancelledAt = null;
            reservation.CancellationReason = null;
        }

        await context.SaveChangesAsync();
        TempData["Message"] = $"Reservation status updated to {status}.";

        return RedirectToAction(nameof(Details), new { id });
    }
}
