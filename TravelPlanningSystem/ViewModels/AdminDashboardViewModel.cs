using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.ViewModels;

public class AdminDashboardViewModel
{

    public decimal AverageFlightPrice { get; set; }
    public int AvailableFlightSeats { get; set; }
    public int FlightSeatCapacity { get; set; }
    public int DelayedFlights { get; set; }
    public int CancelledFlights { get; set; }
    public List<FlightAnalyticsItem> FlightsByAirline { get; set; } = new();
    public List<FlightAnalyticsItem> FlightsByRoute { get; set; } = new();
    public List<FlightCabinAnalyticsItem> SeatsByCabin { get; set; } = new();

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

public class FlightAnalyticsItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class FlightCabinAnalyticsItem
{
    public string Label { get; set; } = string.Empty;
    public int Reserved { get; set; }
    public int Capacity { get; set; }
    public int Available => Math.Max(0, Capacity - Reserved);
    public decimal Percentage { get; set; }
}
