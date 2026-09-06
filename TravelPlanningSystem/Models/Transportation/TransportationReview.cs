using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models.Transportation;

[Table("TransportationReviews")]
public class TransportationReview
{
    [Key]
    public int ReviewId { get; set; }

    [Required]
    [Display(Name = "Trip")]
    public int TripId { get; set; }

    [Required]
    [Display(Name = "User")]
    public Guid UserId { get; set; }

    [Range(1, 5)]
    [Display(Name = "Rating")]
    public int Rating { get; set; }

    [StringLength(500)]
    [Display(Name = "Title")]
    public string? Title { get; set; }

    [StringLength(2000)]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Comment")]
    public string? Comment { get; set; }

    [Display(Name = "Is Visible")]
    public bool IsVisible { get; set; } = true;

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Last Updated")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [ForeignKey(nameof(TripId))]
    public Trip? Trip { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }
}
