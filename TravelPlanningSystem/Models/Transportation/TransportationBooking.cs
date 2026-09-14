using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.Models.Transportation;

[Table("TransportationBookings")]
public class TransportationBooking
{
    [Key]
    public int BookingId { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Booking Reference")]
    public string BookingReference { get; set; } = string.Empty;

    [Required]
    public int TripId { get; set; }

    [ForeignKey(nameof(TripId))]
    public Trip? Trip { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Contact Name")]
    public string ContactName { get; set; } = string.Empty;

    [Required, StringLength(150), EmailAddress]
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required, StringLength(30), Phone]
    [Display(Name = "Contact Phone")]
    public string ContactPhone { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseFareTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaggageFeeTotal { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal InsuranceFeeTotal { get; set; } = 0;

    [StringLength(50)]
    public string? PromoCode { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required, StringLength(30)]
    public string BookingStatus { get; set; } = "Confirmed"; // Confirmed, Cancelled, Completed

    [Required, StringLength(30)]
    public string PaymentStatus { get; set; } = "Paid"; // Pending, Paid, Refunded

    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Online Banking (FPX)";

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Reason provided by the passenger when cancelling the booking
    [StringLength(400)]
    public string? CancellationReason { get; set; }

    // Timestamp when the cancellation was executed
    public DateTime? CancelledAt { get; set; }

    public ICollection<TransportationPassenger> Passengers { get; set; } = new List<TransportationPassenger>();
}