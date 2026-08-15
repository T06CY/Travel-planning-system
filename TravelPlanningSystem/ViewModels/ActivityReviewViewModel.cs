using System.ComponentModel.DataAnnotations;

namespace TravelPlanningSystem.ViewModels;

public class ActivityReviewViewModel
{
    public int ActivityBookingId { get; set; }
    public int ActivityReviewId { get; set; }
    public string ActivityName { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [Required, StringLength(800, MinimumLength = 10)]
    public string Comment { get; set; } = string.Empty;
}
