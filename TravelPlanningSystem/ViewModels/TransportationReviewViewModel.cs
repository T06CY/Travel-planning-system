using System.ComponentModel.DataAnnotations;

namespace TravelPlanningSystem.ViewModels;

public class TransportationReviewViewModel
{
    [Required]
    public int TripId { get; set; }

    [Required(ErrorMessage = "Please select a star rating (1 to 5 stars).")]

    public int Rating { get; set; } = 5;

    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
    [Display(Name = "Review Title")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Please write a few words about your journey.")]
    [StringLength(1000, ErrorMessage = "Review comment cannot exceed 1000 characters.")]
    [Display(Name = "Your Experience")]
    public string Comment { get; set; } = string.Empty;
}