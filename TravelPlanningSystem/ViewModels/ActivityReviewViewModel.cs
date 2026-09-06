using System.ComponentModel.DataAnnotations;

namespace TravelPlanningSystem.ViewModels;

public class ActivityReviewViewModel
{
    public int ActivityBookingId { get; set; }

    public int ActivityReviewId { get; set; }

    public string ActivityName { get; set; } = string.Empty;


    [Range(
        1,
        5,
        ErrorMessage = "Please select a rating."
    )]
    public int Rating { get; set; }


    [Required(
        ErrorMessage = "Please enter your comment."
    )]
    [StringLength(
        800,
        MinimumLength = 10,
        ErrorMessage = "Comment must be between 10 and 800 characters."
    )]
    public string Comment { get; set; } = string.Empty;
}