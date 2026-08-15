using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

public enum ActivityBookingStatus { Pending, Confirmed, Completed, Cancelled }

[Table("ActivityBookings")]
public class ActivityBooking
{
    [Key]
    public int ActivityBookingId { get; set; }

    [Required, StringLength(20)]
    public string BookingReference { get; set; } = string.Empty;

    // Replace this with the shared Member/AppUser foreign key after security integration.
    public int UserId { get; set; }

    [Required]
    public int ActivitySessionId { get; set; }
    public ActivitySession? ActivitySession { get; set; }

    [Range(1, 500)]
    public int ParticipantCount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PricePerPerson { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    public DateTime BookingDate { get; set; } = DateTime.Now;

    [Required, StringLength(100)]
    public string ContactName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(120)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string ContactPhone { get; set; } = string.Empty;

    public ActivityBookingStatus BookingStatus { get; set; } = ActivityBookingStatus.Confirmed;

    [StringLength(400)]
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    public ActivityReview? Review { get; set; }
}
