using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

public class AdminDashboardController : Controller
{
    private readonly AppDbContext _context;

    public AdminDashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel
        {
            TotalActivities = await _context.Activities.CountAsync(),

            ActiveActivities = await _context.Activities
                .CountAsync(a => a.IsActive),

            TotalBookings = await _context.ActivityBookings.CountAsync(),

            UpcomingSessions = await _context.ActivitySessions
                .CountAsync(s =>
                    s.SessionDate >= DateTime.Today &&
                    s.IsActive),

            TotalRevenue = await _context.ActivityBookings
                .Where(b =>
                    b.BookingStatus != ActivityBookingStatus.Cancelled)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0,

            PendingBookings = await _context.ActivityBookings
                .CountAsync(b =>
                    b.BookingStatus == ActivityBookingStatus.Pending),

            ConfirmedBookings = await _context.ActivityBookings
                .CountAsync(b =>
                    b.BookingStatus == ActivityBookingStatus.Confirmed),

            CompletedBookings = await _context.ActivityBookings
                .CountAsync(b =>
                    b.BookingStatus == ActivityBookingStatus.Completed),

            CancelledBookings = await _context.ActivityBookings
                .CountAsync(b =>
                    b.BookingStatus == ActivityBookingStatus.Cancelled),

            RecentBookings = await _context.ActivityBookings
                .AsNoTracking()
                .Include(b => b.ActivitySession)
                    .ThenInclude(s => s!.Activity)
                .OrderByDescending(b => b.BookingDate)
                .Take(6)
                .ToListAsync()
        };

        return View(model);
    }
}