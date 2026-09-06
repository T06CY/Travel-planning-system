using Microsoft.AspNetCore.Authorization;
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

    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel
        {
            // =====================================================
            // FLIGHT MANAGEMENT
            // =====================================================

            TotalFlights = await _context.Flights.CountAsync(),

            ActiveFlights = await _context.Flights
                .CountAsync(f => f.IsActive),

            FlightBookings = await _context.FlightBookings.CountAsync(),

            ReservedFlightSeats = await _context.FlightPassengers
                .CountAsync(p =>
                    p.FlightBooking != null &&
                    p.FlightBooking.Status != FlightBookingStatus.Cancelled),

            FlightRevenue = await _context.FlightBookings
                .Where(b =>
                    b.Status != FlightBookingStatus.Cancelled)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0,

            FlightPendingBookings = await _context.FlightBookings
                .CountAsync(b =>
                    b.Status == FlightBookingStatus.Pending),

            FlightConfirmedBookings = await _context.FlightBookings
                .CountAsync(b =>
                    b.Status == FlightBookingStatus.Confirmed),

            FlightCompletedBookings = await _context.FlightBookings
                .CountAsync(b =>
                    b.Status == FlightBookingStatus.Completed),

            FlightCancelledBookings = await _context.FlightBookings
                .CountAsync(b =>
                    b.Status == FlightBookingStatus.Cancelled),

            RecentFlightBookings = await _context.FlightBookings
                .AsNoTracking()
                .Include(b => b.Segments)
                    .ThenInclude(s => s.Flight)
                .OrderByDescending(b => b.BookingDate)
                .Take(5)
                .ToListAsync(),


            // =====================================================
            // ACTIVITY MANAGEMENT
            // =====================================================

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


            // =====================================================
            // HOTEL MANAGEMENT
            // =====================================================

            TotalHotelRooms = await _context.HotelRooms.CountAsync(),

            ActiveHotelRooms = await _context.HotelRooms
                .CountAsync(r => r.IsActive),

            TotalHotelReservations = await _context.HotelReservations.CountAsync(),

            UpcomingHotelReservations = await _context.HotelReservations
                .CountAsync(r =>
                    r.CheckInDate >= DateTime.Today &&
                    r.ReservationStatus != HotelReservationStatus.Cancelled),

            HotelRevenue = await _context.HotelReservations
                .Where(r =>
                    r.ReservationStatus != HotelReservationStatus.Cancelled)
                .SumAsync(r => (decimal?)r.TotalAmount) ?? 0,

            ConfirmedHotelReservations = await _context.HotelReservations
                .CountAsync(r =>
                    r.ReservationStatus == HotelReservationStatus.Confirmed),

            CompletedHotelReservations = await _context.HotelReservations
                .CountAsync(r =>
                    r.ReservationStatus == HotelReservationStatus.Completed),

            CancelledHotelReservations = await _context.HotelReservations
                .CountAsync(r =>
                    r.ReservationStatus == HotelReservationStatus.Cancelled),


            // =====================================================
            // RECENT ACTIVITY BOOKINGS
            // =====================================================

            RecentBookings = await _context.ActivityBookings
                .AsNoTracking()
                .Include(b => b.ActivitySession)
                    .ThenInclude(s => s!.Activity)
                .OrderByDescending(b => b.BookingDate)
                .Take(6)
                .ToListAsync(),


            // =====================================================
            // RECENT HOTEL RESERVATIONS
            // =====================================================

            RecentHotelReservations = await _context.HotelReservations
                .AsNoTracking()
                .Include(r => r.HotelRoom)
                .OrderByDescending(r => r.ReservationDate)
                .Take(6)
                .ToListAsync()
        };


        // =====================================================
        // USERS
        // =====================================================

        // Load users and staff for admin overview (limit to 10 each)
        model.Users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(10)
            .ToListAsync();


        // =====================================================
        // STAFF USERS
        // =====================================================

        model.StaffUsers = await _context.StaffUsers
            .AsNoTracking()
            .Include(s => s.StaffRole)
            .OrderByDescending(s => s.AccessLevel)
            .ThenBy(s => s.LastName)
            .Take(10)
            .ToListAsync();

        return View(model);
    }
}