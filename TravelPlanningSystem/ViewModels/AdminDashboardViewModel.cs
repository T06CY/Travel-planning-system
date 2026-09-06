using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.ViewModels;

public class AdminDashboardViewModel
{

    public int TotalFlights { get; set; }

    public int ActiveFlights { get; set; }

    public int FlightBookings { get; set; }

    public int ReservedFlightSeats { get; set; }

    public decimal FlightRevenue { get; set; }

    public int FlightPendingBookings { get; set; }

    public int FlightConfirmedBookings { get; set; }

    public int FlightCompletedBookings { get; set; }

    public int FlightCancelledBookings { get; set; }

    public List<FlightBooking> RecentFlightBookings { get; set; }
        = new List<FlightBooking>();


    public int TotalActivities { get; set; }

    public int ActiveActivities { get; set; }

    public int TotalBookings { get; set; }

    public int UpcomingSessions { get; set; }

    public decimal TotalRevenue { get; set; }

    public int PendingBookings { get; set; }

    public int ConfirmedBookings { get; set; }

    public int CompletedBookings { get; set; }

    public int CancelledBookings { get; set; }

    public List<ActivityBooking> RecentBookings { get; set; }
        = new List<ActivityBooking>();


    public int TotalHotelRooms { get; set; }

    public int ActiveHotelRooms { get; set; }

    public int TotalHotelReservations { get; set; }

    public int UpcomingHotelReservations { get; set; }

    public decimal HotelRevenue { get; set; }

    public int PendingHotelReservations { get; set; }

    public int ConfirmedHotelReservations { get; set; }

    public int CompletedHotelReservations { get; set; }

    public int CancelledHotelReservations { get; set; }

    public List<HotelReservation> RecentHotelReservations { get; set; }
        = new List<HotelReservation>();


    // List of application users for admin overview
    public List<ApplicationUser> Users { get; set; }
        = new List<ApplicationUser>();


    // List of staff users for admin overview
    public List<StaffUser> StaffUsers { get; set; }
        = new List<StaffUser>();
}