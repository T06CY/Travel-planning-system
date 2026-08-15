using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("ActivityReviews")]
public class ActivityReview
{
    [Key]
    public int ActivityReviewId { get; set; }

    [Required]
    public int ActivityId { get; set; }
    public Activity? Activity { get; set; }

    [Required]
    public int ActivityBookingId { get; set; }
    public ActivityBooking? ActivityBooking { get; set; }

    public int UserId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, StringLength(800)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public bool IsVisible { get; set; } = true;
}
