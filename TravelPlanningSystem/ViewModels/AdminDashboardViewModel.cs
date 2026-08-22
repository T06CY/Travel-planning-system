using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.ViewModels;

public class AdminDashboardViewModel
{
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

    // List of application users for admin overview
    public List<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    // List of staff users for admin overview
    public List<StaffUser> StaffUsers { get; set; } = new List<StaffUser>();
}