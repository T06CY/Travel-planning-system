using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.ViewModels;

public class ActivityBookingDetailsViewModel
{
    public required ActivityBooking Booking { get; set; }
    public bool CanCancel { get; set; }
    public bool CanReview { get; set; }
    public ActivityReview? ExistingReview { get; set; }
}
