using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminActivityBookingsController(
    AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(
        string? search,
        ActivityBookingStatus? status)
    {
        var query =
            context.ActivityBookings
                .AsNoTracking()
                .Include(b => b.ActivitySession)!
                    .ThenInclude(s => s!.Activity)
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(b =>
                b.BookingReference.Contains(search) ||
                b.ContactName.Contains(search) ||
                b.ContactEmail.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(b =>
                b.BookingStatus == status.Value);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;

        var bookings =
            await query
                .OrderByDescending(b =>
                    b.BookingDate)
                .ToListAsync();

        return View(bookings);
    }


    public async Task<IActionResult> Details(int id)
    {
        var booking =
            await context.ActivityBookings
                .AsNoTracking()
                .Include(b =>
                    b.ActivitySession)!
                    .ThenInclude(s =>
                        s!.Activity)
                .Include(b =>
                    b.Review)
                .FirstOrDefaultAsync(b =>
                    b.ActivityBookingId == id);

        if (booking == null)
        {
            return NotFound();
        }

        return View(booking);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        ActivityBookingStatus status,
        string? cancellationReason)
    {
        var booking =
            await context.ActivityBookings
                .Include(b =>
                    b.ActivitySession)
                .FirstOrDefaultAsync(b =>
                    b.ActivityBookingId == id);

        if (booking == null ||
            booking.ActivitySession == null)
        {
            return NotFound();
        }

        var session =
            booking.ActivitySession;

        var oldStatus =
            booking.BookingStatus;


        if (
            status ==
            ActivityBookingStatus.Cancelled &&
            string.IsNullOrWhiteSpace(
                cancellationReason)
        )
        {
            TempData["Error"] =
                "Please provide a cancellation reason.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        if (
            oldStatus !=
                ActivityBookingStatus.Cancelled &&
            status ==
                ActivityBookingStatus.Cancelled
        )
        {
            session.AvailableSlots =
                Math.Min(
                    session.Capacity,
                    session.AvailableSlots +
                    booking.ParticipantCount);
        }


        if (
            oldStatus ==
                ActivityBookingStatus.Cancelled &&
            status !=
                ActivityBookingStatus.Cancelled
        )
        {
            if (
                session.AvailableSlots <
                booking.ParticipantCount
            )
            {
                TempData["Error"] =
                    "There are not enough available slots to reactivate this booking.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            session.AvailableSlots -=
                booking.ParticipantCount;
        }


        booking.BookingStatus =
            status;


        if (
            status ==
            ActivityBookingStatus.Cancelled
        )
        {
            booking.CancelledAt =
                DateTime.Now;

            booking.CancellationReason =
                cancellationReason?.Trim();
        }
        else
        {
            booking.CancelledAt =
                null;

            booking.CancellationReason =
                null;
        }


        await context.SaveChangesAsync();


        TempData["Message"] =
            $"Booking status updated to {status} successfully.";


        return RedirectToAction(
            nameof(Details),
            new { id });
    }
}