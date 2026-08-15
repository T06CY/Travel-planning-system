using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.Controllers;

public class AdminActivityBookingsController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? search, ActivityBookingStatus? status)
    {
        var query = context.ActivityBookings.AsNoTracking().Include(b => b.ActivitySession)!.ThenInclude(s => s!.Activity).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(b => b.BookingReference.Contains(search) || b.ContactName.Contains(search) || b.ContactEmail.Contains(search));
        if (status.HasValue) query = query.Where(b => b.BookingStatus == status);
        ViewBag.Search = search; ViewBag.Status = status;
        return View(await query.OrderByDescending(b => b.BookingDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    { var b = await context.ActivityBookings.AsNoTracking().Include(x => x.ActivitySession)!.ThenInclude(s => s!.Activity).Include(x => x.Review).FirstOrDefaultAsync(x => x.ActivityBookingId == id); return b is null ? NotFound() : View(b); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ActivityBookingStatus status)
    {
        var b = await context.ActivityBookings.Include(x => x.ActivitySession).FirstOrDefaultAsync(x => x.ActivityBookingId == id); if (b?.ActivitySession is null) return NotFound();
        if (b.BookingStatus != ActivityBookingStatus.Cancelled && status == ActivityBookingStatus.Cancelled) b.ActivitySession.AvailableSlots = Math.Min(b.ActivitySession.Capacity, b.ActivitySession.AvailableSlots + b.ParticipantCount);
        b.BookingStatus = status; if (status == ActivityBookingStatus.Cancelled) { b.CancelledAt = DateTime.Now; b.CancellationReason ??= "Cancelled by administrator"; }
        await context.SaveChangesAsync(); return RedirectToAction(nameof(Details), new { id });
    }
}

